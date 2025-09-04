# Chaussée Neuve — 3 fenêtres liées (WPF .NET 8)

- **Fenêtre 1** (Slide 1) : Choix du mode (Expert / Automatique), bouton **Annuler**.
- **Fenêtre 2** (Slide 2) : Informations du projet (Titre, Auteur, Emplacement + **...** pour parcourir, Description).
  - Titre dynamique : *Informations du projet - Mode expert* (ou *Mode automatique*).
  - Validation **CRÉER** vérifie que le **Titre** n'est pas vide.
- **Fenêtre 3** (Slide 3) : Accueil, en-tête `BENIROUTE — Projet - <Nom>`, barre latérale qui **s'élargit au survol** et affiche les libellés.

Les 3 fenêtres **communiquent** via `AppState.CurrentProject` (données partagées).
