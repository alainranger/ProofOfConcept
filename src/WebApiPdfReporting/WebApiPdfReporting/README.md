# WebApiPdfReporting

Exmaple d'api web qui retourne un fichier PDF à partir de code HTML.

Le code est basé sur cette excellente video Youtube [lien](https://youtu.be/XkhQLnO6v-Y?si=nagj4qVtKb7PfOtL)

Le projet utilise 2 paquetage nuget [Handlebars.Net](https://www.nuget.org/packages/Handlebars.Net) pour créer un modèle HTML et [PuppeteerSharp](https://www.nuget.org/packages/PuppeteerSharp)(Playwright de Microsoft est une librairie semblabe) dans lequel on charge le HTML et converti le rendu en PDF.
