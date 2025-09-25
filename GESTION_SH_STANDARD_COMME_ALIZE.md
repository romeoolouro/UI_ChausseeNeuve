# Gestion des valeurs Sh "standard" - Simulation d'Aliz�

## Contexte

Dans Aliz�, toutes les valeurs Sh des mat�riaux bitumineux sont initialement affich�es comme "standard" et peuvent �tre remplies automatiquement selon les r�gles de la norme NF P98-086.

## Impl�mentation r�alis�e

### 1. Mod�le de donn�es (MaterialItem)

**Nouvelles propri�t�s :**
- `ShStatus` : "standard" | "filled" - Indique le statut de la valeur Sh
- `ShDisplay` : Propri�t� calcul�e qui affiche "standard" ou la valeur num�rique

**Nouvelle m�thode :**
- `FillShFromStandard()` : Remplit automatiquement selon les r�gles :
  - eb-gb* : Sh = 0.30 (Graves bitumes)
  - eb-eme* : Sh = 0.25 (EME)  
  - Autres (BBSG, BBME, etc.) : Sh = 0.25

### 2. Service de donn�es (MaterialDataService)

**Initialisation :**
- Tous les mat�riaux bitumineux ont `Sh = null` et `ShStatus = "standard"`
- Seuls les mat�riaux MTLH gardent leurs valeurs Sh num�riques directes

**Nouvelle m�thode :**
- `FillStandardShValues()` : Remplit toutes les valeurs "standard" d'une collection

### 3. Interface utilisateur (MaterialSelectionControl)

**Affichage :**
- Colonne Sh affiche "standard" ou la valeur num�rique via `ShDisplay`

**Interactions :**
- **Double-clic sur cellule Sh** : Remplit automatiquement cette valeur sp�cifique
- **Bouton "?? Remplir Sh"** : Remplit toutes les valeurs "standard" de la cat�gorie MB

## Comportement attendu

### �tat initial (comme Aliz�)
```
???????????????????????????????????????
? Mat�riau    ? SN   ? Sh   ? Kc      ?
???????????????????????????????????????
? eb-bbsg1    ? 0.25 ? std  ? 1.1     ?
? eb-bbsg2    ? 0.25 ? std  ? 1.1     ?
? eb-gb2      ? 0.3  ? std  ? 1.3     ?
? eb-eme1     ? 0.25 ? std  ? 1.0     ?
???????????????????????????????????????
```

### Apr�s remplissage automatique
```
???????????????????????????????????????
? Mat�riau    ? SN   ? Sh   ? Kc      ?
???????????????????????????????????????
? eb-bbsg1    ? 0.25 ? 0.25 ? 1.1     ?
? eb-bbsg2    ? 0.25 ? 0.25 ? 1.1     ?
? eb-gb2      ? 0.3  ? 0.30 ? 1.3     ?
? eb-eme1     ? 0.25 ? 0.25 ? 1.0     ?
???????????????????????????????????????
```

## R�gles de remplissage (conforme NF P98-086)

| Type de mat�riau | Valeur Sh appliqu�e |
|------------------|-------------------|
| Graves bitumes (eb-gb*) | 0.30 |
| EME (eb-eme*) | 0.25 |
| BBSG, BBME, autres | 0.25 |

## Tests de validation

### Test 1 : Affichage initial
1. Ouvrir Biblioth�que > NFP98_086_2019 > MB
2. V�rifier que toutes les valeurs Sh affichent "standard"

### Test 2 : Double-clic individuel
1. Double-cliquer sur une cellule Sh "standard"
2. V�rifier que la valeur se remplit selon les r�gles
3. V�rifier que la cellule affiche maintenant la valeur num�rique

### Test 3 : Remplissage global
1. Cliquer sur le bouton "?? Remplir Sh"
2. Confirmer dans la bo�te de dialogue
3. V�rifier que toutes les valeurs "standard" sont remplies
4. V�rifier que les valeurs respectent les r�gles par type de mat�riau

### Test 4 : Coh�rence avec Aliz�
1. Comparer l'affichage initial avec votre premi�re image d'Aliz�
2. Comparer l'affichage apr�s remplissage avec votre deuxi�me image d'Aliz�
3. V�rifier que les valeurs num�riques correspondent exactement

## Avantages de cette approche

? **Conformit� Aliz�** : Comportement identique au logiciel de r�f�rence  
? **Flexibilit�** : Remplissage individuel ou global  
? **Tra�abilit�** : Distinction claire entre valeurs "standard" et "remplies"  
? **R�gles normatives** : Application automatique des r�gles NF P98-086  
? **Interface intuitive** : Double-clic + bouton d�di�  

## Extensions possibles

- Gestion de "standard" pour d'autres param�tres (Kc, Kd, etc.)
- Sauvegarde/restauration de l'�tat "standard"
- Export des valeurs avec indication du mode de remplissage
- Validation des r�gles selon le contexte d'utilisation