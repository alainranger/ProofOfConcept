# EplToPdfConverter.WebUsb

Projet web ASP.NET Core qui permet de :

1. Generer un exemple EPL.
2. Convertir EPL vers ZPL via l'API backend (reutilise `EplToPdfConverter`).
3. Envoyer le ZPL vers une imprimante Zebra en USB via WebUSB (frontend navigateur).

## Prerequis

- .NET SDK 10
- Un navigateur Chromium (Edge/Chrome) compatible WebUSB
- L'application executee sur `https://` ou `http://localhost`

## Lancer le projet

```powershell
dotnet run --project .\EplToPdfConverter.WebUsb\EplToPdfConverter.WebUsb.csproj
```

Ouvrir l'URL locale affichee dans la console.

## Utilisation

1. Cliquer sur "Charger un exemple".
2. Cliquer sur "Convertir en ZPL".
3. Cliquer sur "Connecter Zebra USB" et selectionner l'imprimante.
4. Cliquer sur "Imprimer".

## Notes

- Le filtre USB est configure sur le Vendor ID Zebra `0x0A5F`.
- Selon le modele, l'interface/endpoints USB peuvent differer.
- Le navigateur demandera une autorisation explicite pour acceder au peripherique USB.
