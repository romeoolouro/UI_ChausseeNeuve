# Gestion des valeurs Sh "standard" - Simulation d'Alizé

## Contexte

Dans Alizé, toutes les valeurs Sh des matériaux bitumineux sont initialement affichées comme "standard" et peuvent être remplies automatiquement selon les règles de la norme NF P98-086.

## Implémentation réalisée

### 1. Modèle de données (MaterialItem)

**Nouvelles propriétés :**
- `ShStatus` : "standard" | "filled" - Indique le statut de la valeur Sh
- `ShDisplay` : Propriété calculée qui affiche "standard" ou la valeur numérique

**Nouvelle méthode :**
- `FillShFromStandard()` : Remplit automatiquement selon les règles :
  - eb-gb* : Sh = 0.30 (Graves bitumes)
  - eb-eme* : Sh = 0.25 (EME)  
  - Autres (BBSG, BBME, etc.) : Sh = 0.25

### 2. Service de données (MaterialDataService)

**Initialisation :**
- Tous les matériaux bitumineux ont `Sh = null` et `ShStatus = "standard"`
- Seuls les matériaux MTLH gardent leurs valeurs Sh numériques directes

**Nouvelle méthode :**
- `FillStandardShValues()` : Remplit toutes les valeurs "standard" d'une collection

### 3. Interface utilisateur (MaterialSelectionControl)

**Affichage :**
- Colonne Sh affiche "standard" ou la valeur numérique via `ShDisplay`

**Interactions :**
- **Double-clic sur cellule Sh** : Remplit automatiquement cette valeur spécifique
- **Bouton "?? Remplir Sh"** : Remplit toutes les valeurs "standard" de la catégorie MB

## Comportement attendu

### État initial (comme Alizé)
```
???????????????????????????????????????
? Matériau    ? SN   ? Sh   ? Kc      ?
???????????????????????????????????????
? eb-bbsg1    ? 0.25 ? std  ? 1.1     ?
? eb-bbsg2    ? 0.25 ? std  ? 1.1     ?
? eb-gb2      ? 0.3  ? std  ? 1.3     ?
? eb-eme1     ? 0.25 ? std  ? 1.0     ?
???????????????????????????????????????
```

### Après remplissage automatique
```
???????????????????????????????????????
? Matériau    ? SN   ? Sh   ? Kc      ?
???????????????????????????????????????
? eb-bbsg1    ? 0.25 ? 0.25 ? 1.1     ?
? eb-bbsg2    ? 0.25 ? 0.25 ? 1.1     ?
? eb-gb2      ? 0.3  ? 0.30 ? 1.3     ?
? eb-eme1     ? 0.25 ? 0.25 ? 1.0     ?
???????????????????????????????????????
```

## Règles de remplissage (conforme NF P98-086)

| Type de matériau | Valeur Sh appliquée |
|------------------|-------------------|
| Graves bitumes (eb-gb*) | 0.30 |
| EME (eb-eme*) | 0.25 |
| BBSG, BBME, autres | 0.25 |

## Tests de validation

### Test 1 : Affichage initial
1. Ouvrir Bibliothèque > NFP98_086_2019 > MB
2. Vérifier que toutes les valeurs Sh affichent "standard"

### Test 2 : Double-clic individuel
1. Double-cliquer sur une cellule Sh "standard"
2. Vérifier que la valeur se remplit selon les règles
3. Vérifier que la cellule affiche maintenant la valeur numérique

### Test 3 : Remplissage global
1. Cliquer sur le bouton "?? Remplir Sh"
2. Confirmer dans la boîte de dialogue
3. Vérifier que toutes les valeurs "standard" sont remplies
4. Vérifier que les valeurs respectent les règles par type de matériau

### Test 4 : Cohérence avec Alizé
1. Comparer l'affichage initial avec votre première image d'Alizé
2. Comparer l'affichage après remplissage avec votre deuxième image d'Alizé
3. Vérifier que les valeurs numériques correspondent exactement

## Avantages de cette approche

? **Conformité Alizé** : Comportement identique au logiciel de référence  
? **Flexibilité** : Remplissage individuel ou global  
? **Traçabilité** : Distinction claire entre valeurs "standard" et "remplies"  
? **Règles normatives** : Application automatique des règles NF P98-086  
? **Interface intuitive** : Double-clic + bouton dédié  

## Extensions possibles

- Gestion de "standard" pour d'autres paramètres (Kc, Kd, etc.)
- Sauvegarde/restauration de l'état "standard"
- Export des valeurs avec indication du mode de remplissage
- Validation des règles selon le contexte d'utilisation