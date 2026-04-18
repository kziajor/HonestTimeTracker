# HonestTimeTracker — Instrukcje projektu

## Cel aplikacji

Desktopowa aplikacja .NET do śledzenia czasu pracy nad zadaniami, wzorowana na konsolowej wersji z katalogu `../ConsoleTimeTracker`. Docelowo zintegrowana z TFS (self-hosted / Azure DevOps Server).

## Stack technologiczny

- **Platforma:** .NET 10 — aplikacja desktopowa WPF
- **Baza danych:** SQLite (pojedynczy plik `.db`)
- **ORM:** EF Core z providerem SQLite
- **CI firmy:** Honest IT — kolory `#fbcb18` / `#fede12`, font Exo, czarny tekst, logo SVG

## Model danych

### Project (Projekt)
- Id, Name, Closed
- Opcjonalne powiązanie z projektem TFS (kolekcja + ID projektu w TFS)

### Task (Zadanie)
- Id, Title, PlannedTime (minuty), SpentTime (minuty), Closed
- RelProjectId → Project
- Opcjonalne powiązanie z work item TFS: TfsWorkItemId (nullable)

### Record (Rekord / sesja pracy)
- Id, StartedAt (DateTime), FinishedAt (DateTime?), MinutesSpent, Comment
- RelTaskId → Task

### TfsCollection (Kolekcja TFS)
- Id, Name, Url
- Powiązane projekty TFS (lista Project z TfsProjectId)

### Settings (Ustawienia)
- DbFilePath
- DailyWorkHours (standardowy czas pracy w ciągu dnia — godziny)

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
- Definiowanie kolekcji TFS (nazwa + URL)
- Przypisywanie projektów aplikacji do projektów TFS (po TFS Project ID)
- Autentykacja przez zalogowanego użytkownika Windows (AD / NTLM / Kerberos) — `VssCredentials` z `UseDefaultCredentials`; brak dodatkowego logowania

### Integracja z TFS (synchronizacja)
- Zamykanie zadań w aplikacji, jeśli powiązany work item w TFS jest zamknięty
- Import nowych work itemów przypisanych do aktualnie zalogowanego użytkownika Windows jako nowe zadania

## Wzorzec architektoniczny

Clean Architecture:
```
src/HonestTimeTracker.Domain
src/HonestTimeTracker.Application
src/HonestTimeTracker.Infrastructure
src/HonestTimeTracker.Desktop        ← UI (WPF/MAUI)
tests/UnitTests
tests/IntegrationTests
```

CQRS z własną implementacją (bez MediatR) — zgodnie z globalnymi konwencjami .NET.

## UX / UI

- Zgodność z kolorystyką Honest IT (primary `#fbcb18`, contrast `#A0522D` dla interaktywnych elementów, tło białe lub żółte)
- Font Exo
- Czytelna nawigacja: panel boczny lub zakładki (Projekty, Dziś, Rekordy, Raporty, Ustawienia)
- Pływające okno timera: minimalistyczne, nieinwazyjne, zawsze na wierzchu

## Źródło inspiracji

Konsolowa wersja aplikacji: `../ConsoleTimeTracker`
- Encje: `Project`, `Task`, `Record` — zachować kompatybilność koncepcyjną
