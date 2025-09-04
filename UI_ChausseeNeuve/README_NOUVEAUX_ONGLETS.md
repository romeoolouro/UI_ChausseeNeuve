# Guide d'utilisation - Nouveaux Onglets

## Onglets ajoutés

L'application UI_ChausseeNeuve a été enrichie avec deux nouveaux onglets accessibles depuis l'app bar :

### 1. Valeurs Admissibles
- **Accès** : Cliquer sur "valeurs" dans la barre de navigation
- **Fonctionnalités** :
  - Mode de calcul manuel ou saisie directe
  - Calcul du trafic cumulé basé sur MJA, taux d'accroissement et durée de service
  - Gestion des couches de chaussée
  - Validation automatique des données saisies

### 2. Résultats
- **Accès** : Cliquer sur "resultats" dans la barre de navigation  
- **Fonctionnalités** :
  - Visualisation des résultats de calculs
  - Affichage des contraintes et déformations par couche
  - Validation de la structure (valide/non valide)
  - Export et impression des résultats

## Interface utilisateur moderne

Les nouveaux onglets suivent le design moderne de l'application avec :
- Interface sombre avec couleurs d'accent
- Cartes avec bordures arrondies
- Animations et transitions fluides
- Responsive design
- Validation visuelle en temps réel

## Navigation

- Les onglets sont accessibles depuis la barre de navigation horizontale
- Le logo central disparaît lors de la navigation vers un onglet
- Bouton "Retour" pour revenir à la sélection de mode
- Navigation fluide entre les différentes sections

## Calculs et données

### Valeurs admissibles
- Calcul automatique du trafic cumulé selon les formules standard
- Support des taux d'accroissement arithmétique et géométrique
- Validation des paramètres d'entrée
- Données d'exemple pré-chargées pour démonstration

### Résultats
- Affichage des résultats par couche
- Validation automatique selon les critères
- Indicateurs visuels de conformité
- Données de démonstration intégrées

## Dépannage

Si l'application ne démarre pas :
1. Vérifier que les dépendances .NET sont installées
2. Recompiler avec `dotnet build UI_ChausseeNeuve.sln`
3. Lancer avec `dotnet run --project UI_ChausseeNeuve`

Si un onglet crash :
1. Vérifier les logs dans la console de développement
2. S'assurer que tous les converters sont bien référencés
3. Vérifier que les ViewModels s'initialisent correctement

## Développement futur

Les placeholders sont en place pour :
- Intégration avec le moteur de calcul principal
- Connexion avec la base de données des matériaux
- Export des résultats en PDF/Excel
- Paramètres de configuration avancés

## Architecture technique

- **MVVM** : Pattern Model-View-ViewModel respecté
- **Data Binding** : Liaison bidirectionnelle des données
- **Commands** : Pattern Command pour les actions utilisateur
- **Converters** : Conversion automatique des types de données
- **Styles** : Thèmes centralisés dans Theme.xaml
