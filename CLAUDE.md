# HonestTimeTracker — Instrukcje projektu

## Cel aplikacji

Desktopowa aplikacja .NET do śledzenia czasu pracy nad zadaniami, wzorowana na konsolowej wersji z katalogu `../ConsoleTimeTracker`. Docelowo zintegrowana z TFS (self-hosted / Azure DevOps Server).

## Język interfejsu

Cały interfejs użytkownika (UI) jest w języku **angielskim** — wszystkie etykiety, nagłówki, przyciski, komunikaty błędów i okna dialogowe.

## Stack technologiczny

- **Platforma:** .NET 10 — aplikacja desktopowa WPF
- **Baza danych:** SQLite (pojedynczy plik `.db`)
- **ORM:** EF Core z providerem SQLite
- **CI firmy:** Honest IT — kolory `#fbcb18` / `#fede12`, font Exo, czarny tekst, logo SVG

## Model danych

### Project (Projekt)
- Id, Name, Closed
- Kolumny TfsCollectionId / TfsProjectId istnieją w bazie, ale nie są używane (brak UI mapowania) — kilka projektów aplikacji może dotyczyć jednego projektu TFS (podział na typy prac: implementacja, testy, analiza…)

### Task (Zadanie)
- Id, Title, PlannedTime (minuty), SpentTime (minuty), Closed
- RelProjectId → Project
- Opcjonalne powiązanie z work item TFS: TfsWorkItemId (nullable) + TfsCollectionId (nullable, FK → TfsCollection, SetNull)
- Numer work itemu jest unikalny tylko w obrębie kolekcji — powiązanie identyfikuje para (TfsCollectionId, TfsWorkItemId); `TfsCollectionId = null` oznacza "auto-detect" (uzupełniane przy synchronizacji)

### Record (Rekord / sesja pracy)
- Id, StartedAt (DateTime), FinishedAt (DateTime?), MinutesSpent, Comment
- RelTaskId → Task

### TfsCollection (Kolekcja TFS)
- Id, Name (unikalna), Url (unikalny, bez końcowego `/`), EncryptedPat, Enabled
- `EncryptedPat` — token PAT zaszyfrowany DPAPI (`ProtectedData`, `CurrentUser`) przez `ISecretProtector`; nigdy nie zapisywany jawnie

### TfsImportPreference (Pamięć wyboru projektu przy imporcie)
- Id, TfsCollectionId, TfsProjectName (`System.TeamProject`, NOCASE, unikalne w kolekcji), LastProjectId
- Ostatnio wybrany projekt aplikacji dla danego projektu TFS — preselekcja w oknie importu

### Settings (Ustawienia)
- DbFilePath
- DailyWorkHours (standardowy czas pracy w ciągu dnia — godziny)
- DefaultTaskPlannedHours, ShowFloatingTimer, FloatingTimerLeft/Top
- VerboseTfsLogging (pełne logowanie żądań/odpowiedzi TFS na poziomie Debug)

## Funkcjonalności

### Zarządzanie projektami i zadaniami
- CRUD projektów i zadań
- Oznaczanie projektu/zadania jako zakończonego
- Zadania można przypisać do projektu

### Lista "dziś"
- Użytkownik zaznacza zadania, nad którymi chce dziś pracować
- Widok "dzisiejszych" zadań z możliwością uruchomienia timera

### Timer / liczenie czasu
- Przycisk Play przy zadaniu → natychmiastowe utworzenie rekordu w bazie z `StartedAt = now`, `FinishedAt = null`
- Przycisk Stop → uzupełnienie `FinishedAt` i obliczenie `MinutesSpent` na istniejącym rekordzie
- Tylko jedno zadanie może być aktywne jednocześnie
- **Odporność na crash:** przy starcie aplikacji sprawdzane są rekordy z `FinishedAt = null` — jeśli istnieją, użytkownik jest pytany co z nimi zrobić: zakończyć teraz (FinishedAt = chwila bieżąca) lub odrzucić rekord (usunąć)

### Pływające okno timera
- Małe okno zawsze na wierzchu (Topmost)
- Wyświetla: tytuł zadania, nazwa projektu, czas łączny, czas planowany, czas pozostały
- Widoczne podczas aktywnego pomiaru

### Rekordy pracy
- Ręczne dodawanie/edycja rekordów (data + godzina start, data + godzina koniec)
- Lista rekordów dla zadania

### Raporty
1. **Raport zadania** — czas łączny + lista rekordów dla wybranego zadania
2. **Raport dnia** — lista rekordów w wybranym dniu + podsumowanie czasu pracy
3. **Raport okresu** — suma przepracowanych godzin w wybranym przedziale dat
4. **Raport normy** — nadgodziny lub braki do normy w wybranym miesiącu/roku (względem `DailyWorkHours`)

### Konfiguracja TFS
- Serwer: Azure DevOps Server 25H2 (self-hosted), REST API `api-version=7.1` (stała `TfsClient.ApiVersion`)
- Ustawienia → "TFS collections": lista kolekcji (Name, URL, PAT, Enabled, Status testu) z akcjami w wierszu: Test connection, Edit, Delete (soft delete); zapis natychmiastowy, niezależny od "Save settings"
- Autentykacja tokenem PAT per kolekcja (`Authorization: Basic base64(":" + PAT)`), wymagany zakres: Work Items (Read); test połączenia przez `_apis/connectionData` pokazuje właściciela tokenu
- Wyłączona kolekcja (Enabled = false) jest pomijana w synchronizacji
- Klient: własny typowany `TfsClient` na `HttpClient` (`IHttpClientFactory`, timeout 30 s) — bez pakietu `Microsoft.TeamFoundationServer.Client`

### Integracja z TFS (synchronizacja)
Przycisk "Sync with TFS" w widoku Tasks (`SyncWithTfsCommand`), wyłącznie ręcznie, z możliwością anulowania:
1. **Zamykanie zadań** — dla otwartych zadań z TfsWorkItemId sprawdzany jest stan work itemu:
   - kolekcja z `WorkTask.TfsCollectionId`; gdy brak — szukanie we wszystkich włączonych kolekcjach: znaleziony w dokładnie jednej → uzupełnienie TfsCollectionId (samonaprawa), w kilku/żadnej → pominięcie; samonaprawa tylko gdy wszystkie kolekcje odpowiedziały
   - o zamknięciu decyduje **kategoria stanu** (endpoint `workitemtypes/{type}/states`, cache per projekt+typ): Completed i Removed zamykają, Resolved nie zamyka; nierozpoznany stan → pominięcie + wpis w logu
   - zamknięcie automatyczne (`Closed = true`, `ClosedAt = now`); zadanie z aktywnym timerem nie jest zamykane (trafia do podsumowania)
2. **Kandydaci do importu** — WIQL w całej kolekcji: `@Me` (właściciel PAT), typy Task i Bug, stany nie-Completed/Removed (filtr kategorii po stronie klienta)
   - wykluczone work itemy powiązane z dowolnym zadaniem, także zamkniętym i usuniętym (`IgnoreQueryFilters`)
   - tytuł: `{Parent title} - {WorkItem title}` (parent z relacji `System.LinkTypes.Hierarchy-Reverse`); brak/niedostępny parent → sam tytuł; limit 500 znaków (przycinany tytuł parenta)
3. **Okno importu** (`TfsImportDialog`) — panel podsumowania (zamknięte, pominięte, błędy kolekcji) + lista pogrupowana po kolekcji: Add (domyślnie zaznaczone), TFS #, Type, TFS project, Title (edytowalny), Project (wymagany, preselekcja z `TfsImportPreference`, pusty gdy zapamiętany projekt jest zamknięty), Planned [h] (domyślnie `DefaultTaskPlannedHours`), Today; akcje masowe Select all/none i "Set project for selected"
   - "Import selected" → `ImportTfsTasksCommand` w jednym `SaveChanges` (jedna transakcja) razem z aktualizacją preferencji
   - odznaczone work itemy wracają przy kolejnej synchronizacji (brak listy ignorowanych)
4. Brak kandydatów → `MessageBox` z podsumowaniem albo "Everything up to date"
- Błąd jednej kolekcji (401/403 → "PAT invalid or expired", brak połączenia/timeout → "Server unreachable") nie przerywa synchronizacji pozostałych

## Logowanie
- `ILogger<T>` (Microsoft.Extensions.Logging) w Application/Infrastructure, Serilog jako provider (konfiguracja w `AppLogging`)
- Plik: `%AppData%\HonestTimeTracker\logs\htt-.log`, rolowany dziennie, 14 dni retencji; Ustawienia → "Open log folder"
- Kategorie TFS (`HonestTimeTracker.Application.Tfs`, `HonestTimeTracker.Infrastructure.Tfs`) mają przełączany poziom: Information / Debug ("Verbose TFS logging")
- Logowane: błędy HTTP z body (max 4 KB), wyjątki sieciowe, błędy deserializacji JSON, nierozpoznane stany/kategorie, niejednoznaczne ID; `TfsLoggingHandler` nigdy nie loguje nagłówków — PAT nie może trafić do logu
- Niełapane wyjątki (`DispatcherUnhandledException`, `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`) są logowane

## Konwencje trwałości danych

### Soft delete
Wszystkie encje domenowe są usuwane wyłącznie przez soft delete:
- Każda encja posiada pole `bool IsDeleted` (domyślnie `false`).
- Fizyczne usunięcie rekordu (`DELETE`) jest niedozwolone.
- `AppDbContext` konfiguruje globalny query filter `HasQueryFilter(e => !e.IsDeleted)` dla każdej encji — usunięte rekordy są niewidoczne domyślnie.
- Operacje "usuń" w aplikacji ustawiają `IsDeleted = true` i zapisują zmiany.

## Wzorzec architektoniczny

Clean Architecture:
```
src/HonestTimeTracker.Domain
src/HonestTimeTracker.Application
src/HonestTimeTracker.Infrastructure
src/HonestTimeTracker.Desktop        ← UI (WPF/MAUI)
tests/UnitTests                      ← xUnit + NSubstitute, logika Application (fake'i repozytoriów i ITfsClient)
tests/IntegrationTests               ← xUnit, TfsClient na stubie HttpMessageHandler, repozytoria na SQLite in-memory z migracjami
```

## Testy
- Po każdej zmianie w kodzie uruchom testy (`dotnet test HonestTimeTracker.slnx`) i sprawdź, czy wszystkie przechodzą. Zadanie nie jest skończone, dopóki jakikolwiek test nie przechodzi.

CQRS z własną implementacją (bez MediatR) — zgodnie z globalnymi konwencjami .NET.

## UX / UI

- Zgodność z kolorystyką Honest IT (primary `#fbcb18`, contrast `#A0522D` dla interaktywnych elementów, tło białe lub żółte)
- Font Exo
- Czytelna nawigacja: panel boczny lub zakładki (Projekty, Dziś, Rekordy, Raporty, Ustawienia)
- Pływające okno timera: minimalistyczne, nieinwazyjne, zawsze na wierzchu

## Źródło inspiracji

Konsolowa wersja aplikacji: `../ConsoleTimeTracker`
- Encje: `Project`, `Task`, `Record` — zachować kompatybilność koncepcyjną
