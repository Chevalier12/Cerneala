# Cerneala Menu Lab

Proiect nativ Cerneala pentru `Menu`, `MenuItem` si `MenuBar`.

```powershell
dotnet run --project .\Cerneala.MenuLab.csproj
```

Lab-ul include:

- bara de meniu cu submeniuri imbricate;
- meniu vertical de navigare;
- fallback lateral pentru meniul de la marginea dreapta;
- un view model dedicat, cu comenzile legate declarativ direct in markup;
- acelasi `ActionCommand` folosit de `MenuItem` si `Button`;
- `CanExecuteChanged`, `CommandParameter`, `SubmenuOpened` si `SubmenuClosed`.

Proiectul referentiaza direct proiectele Cerneala din radacina repository-ului.
