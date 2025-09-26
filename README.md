# Chaussée Neuve — 3 fenêtres liées (WPF .NET 8)

- **Fenêtre 1** (Slide 1) : Choix du mode (Expert / Automatique), bouton **Annuler**.
- **Fenêtre 2** (Slide 2) : Informations du projet (Titre, Auteur, Emplacement + **...** pour parcourir, Description).
  - Titre dynamique : *Informations du projet - Mode expert* (ou *Mode automatique*).
  - Validation **CRÉER** vérifie que le **Titre** n'est pas vide.
- **Fenêtre 3** (Slide 3) : Accueil, en-tête `BENIROUTE — Projet - <Nom>`, barre latérale qui **s'élargit au survol** et affiche les libellés.

Les 3 fenêtres **communiquent** via `AppState.CurrentProject` (données partagées).

## 🧪 Tests de Synchronisation

Ce projet contient des tests spécifiques pour valider les corrections des problèmes de synchronisation.

### Exécution des Tests
```bash
# Tests à la demande (recommandé)
.\run-synchronization-tests.bat

# Via dotnet CLI
dotnet test UI_ChausseeNeuve.Tests
```

### Configuration Build
- **Debug** : Build normal avec projet de test inclus
- **Debug-NoTests** : Build sans compilation du projet de test  
- **Release** : Build de production

Pour plus de détails, voir [`TESTS_SYNCHRONIZATION_README.md`](./TESTS_SYNCHRONIZATION_README.md)
