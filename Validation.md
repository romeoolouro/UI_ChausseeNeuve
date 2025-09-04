### **Rapport Final Détaillé et Atomique pour le Technicien : Intégration des Règles Métier NF P98-086 et Améliorations Ergonomiques**

**Titre :** Implémentation des Règles de Cohérence, Validation Granulaire et Ergonomie de Saisie selon NF P98-086 - Phase 3 : Exhaustivité des Règles et Résolution de Bugs Visuels.

**Priorité :** **CRITIQUE** (Garantir la conformité normative exhaustive, la robustesse de l'application et l'expérience utilisateur fluide).

Ce rapport intègre toutes les règles extraites de la norme NF P98-086 (sections 3, 7, 8, Annexes C, D, E, F), corrige les omissions précédentes (ex. : épaisseurs minimales modulées par classe PF, détails sur les coefficients ks/kd, règles spécifiques aux interfaces pour sols traités), et restructure les missions en tâches atomiques (une action unique par mission, avec localisation précise dans le code). Il prend en compte l'image de l'UI (affichage des couches avec étiquettes d'épaisseur potentiellement coupées, types de structures "Souple", matériaux comme "BetonBitumineux" et "GNT & Sol", interfaces visibles). Les plages simplifiées sont conservées pour l'implémentation, mais complétées par des valeurs exactes des tableaux normatifs pour référence.

---

#### **1. Statut Actuel et Bug Visuel Persistant (Priorité Secondaire)**

*   **Bug Persistant :** Dans la "Vue en coupe" (Grid.Column="1"), les étiquettes d'épaisseur (ex. "0.04 m") sont coupées verticalement ou masquées par la couche suivante, malgré les tentatives précédentes (ClipToBounds="False" sur ItemsControl/ScrollViewer/parents, Panel.ZIndex=99 sur l'étiquette, déplacement de l'étiquette comme sibling du Border avec CornerRadius, Margin négatif -12).
    *   **Analyse basée sur l'image et le XAML :** Le rognage semble provenir du rendu séquentiel dans l'ItemsControl (StackPanel implicite) où les couches opaques suivantes masquent les étiquettes débordantes. Le ScrollViewer parent clippe aussi malgré ClipToBounds="False", potentiellement à cause d'un ancêtre comme le Border de la carte.
    *   **Action globale :** Les missions suivantes (1.1 à 1.4) décomposent la résolution en étapes atomiques.

---

#### **2. Extraction Exhaustive des Règles Métier de la Norme NF P98-086**

Toutes les règles applicables à l'application sont listées ci-dessous, extraites des sections clés (3.1 pour définitions, 7-8 pour vérifications, Annexes C/D/E/F pour propriétés). Les plages simplifiées sont proposées pour l'implémentation immédiate ; utiliser les tableaux exacts pour des validations avancées futures.

##### **2.1. Concepts Fondamentaux et Définitions (Section 3.1, pages 12-16)**

*   **Chaussée/Structure [3.1.1] :** Ensemble de couches superposées sur la plate-forme.
*   **Couche [3.1.2] :** Élément d'un seul matériau (ex. : Roulement, Base, Fondation).
*   **Assise [3.1.7] :** Partie principale (Base + Fondation), exclut Roulement et Plate-forme.
*   **Plate-forme [3.1.6] :** Surface support (sol naturel ou couche de forme), non une couche structurelle. Module E ≥ 20 MPa, Poisson = 0.35. Matériaux limités à GNT ou sols (pas MTLH/Bitumineux/Béton).
*   **Interface [3.1.20] :** Contact entre couches ; types : collée (déplacements continus), glissante (cisaillement libre), semi-collée (moyenne des deux).

##### **2.2. Classification des Matériaux et Propriétés (Annexes D/E, pages 65-83)**

Utiliser ces plages pour validations automatiques ; auto-corriger aux bornes les plus proches.

| Famille Matériau | Module E (MPa) - Plage Simplifiée [Min ; Max] | Poisson (ν) Fixe | Épaisseur (m) - Plage Simplifiée [Min ; Max] | Détails Exacts des Tableaux |
|------------------|-----------------------------------------------|------------------|----------------------------------------------|-----------------------------|
| **GNT (Graves Non Traitées)** [D.1, E.1] | [100 ; 1000] (Emax: CG1=600, CG2=400, CG3=200 pour souples) | 0.35 | [0.10 ; 0.35] (min 0.10 pour 0/14, 0.15 pour 0/20, 0.20 pour 0/31.5-63) | Tableau D.1: k=3/2.5/2 pour CG1-3 ; Subdivision en sous-couches de 0.25m max, module croissant depuis plate-forme. |
| **MTLH (Matériaux Traités aux Liants Hydrauliques)** [D.2, E.2] | [3000 ; 32000] (ex. Grave Ciment T3=23000, T4=25000 ; Sols T2=7000-9000) | 0.25 | [0.15 ; 0.32] (min 0.15 graves/bétons, 0.20 sols) | Tableaux D.3/D.4/D.5: Classes T2-T5 ; Niveaux AC1/AC2 pour pondération ; b=-0.2 à -0.24, SN=0.5-0.8. |
| **Béton Bitumineux (BB)** [D.3, E.3] | [3000 ; 18000] (ex. GB2=9000-11000, EME1=14000, BBSG=7000) | 0.35 (0.4 si θeq ≥25°C) | [0.05 ; 0.16] (min 0.025 BBTM, 0.06 EME 0/10 ; max 0.08 EME 0/10, 0.16 GB 0/20) | Tableaux D.7-D.12: Classes 2-4 ; Essais NF EN 12697-26 ; kc=1.3-1.5 ; kd=0.5-0.8 ; b=-0.2, SN=0.5. |
| **Béton de Ciment (BC)** [D.4, E.4] | [18000 ; 40000] (BC2=20000, BC3=24000, BC6=40000) | 0.25 | [0.12 ; 0.45] | Tableau D.14: Classes BC2-6 ; kc=1.5 ; kd=0.6-1.0 ; b=-0.06 ; SN=0.5-0.8. |
| **Bibliotheque** | Pas de règles normatives ; utilisateur définit librement. | N/A | N/A | Custom ; pas de validation auto. |

*   **Autres :** Coefficient ks (pour hétérogénéités plate-forme) [C.2]: 0.9 (PF1), 0.8 (PF2), 0.7 (PF3+). kd (discontinuité) varie par matériau/classe [D.2.4/D.4.1].

##### **2.3. Classes de Portance de la Plate-forme (Annexe C.1, Tableau C.1, page 58)**

*   **PFx :** Classes basées sur module E (MPa), Poisson=0.35.
    | Classe | Module E (MPa) |
    |--------|----------------|
    | PF1    | [20 ; 50)     |
    | PF2    | [50 ; 80)     |
    | PF2qs  | [50 ; 80) (qualité spéciale, réglée ±0.015m) |
    | PF3    | [80 ; 120)    |
    | PF4    | ≥200          |
*   **Plage simplifiée pour Plate-forme :** [20 ; Infini]. Épaisseurs min de Fondation/Base modulées par PF (ex. Fondation MTLH: 0.20m en PF2, 0.15m en PF4) [8.5-8.8].

##### **2.4. Règles de Cohérence des Interfaces (Section 8.5.1.3, page 40 ; complété par 8.8.1.1)**

*   **Général :** Dépend des matériaux en contact ; semi-collée = moyenne collée/glissante.
*   **Fondation/Base sur Plate-forme :** Toujours collée.
*   **Base - Fondation (si Base MTLH) :**
    - Glissante : Grave-cendres-volantes-chaux, Grave Ciment T4.
    - Collée : Grave-laitier.
    - Semi-collée : Autres MTLH.
*   **Surface - Base :** Collée (sauf si Base en sol traité : semi-collée).
*   **Spécifiques Rigides [8.8.1.1] :** BAC/BBSG=glissante ; BAC/GB3=semi-collée ; BCg/GB3=semi-collée ; Béton sur MTLH=collée (sauf Grave Ciment T4=glissante).
*   **Fatigue :** Si couche MTLH endommagée, module = 1/5 initial, interface glissante.

##### **2.5. Règles de Cohérence Globale des Structures (Sections 3.1.11-3.1.16, pages 13-14 ; 8.2-8.8)**

Vérifier lors de validation globale ; inclure épaisseurs totales et présences/absences.

*   **Souple [3.1.11, 8.2] :** BB total ≤0.12m ; GNT total ≥0.15m ; Interdiction MTLH/Béton en assises ; Subdivision GNT en sous-couches 0.25m max.
*   **Bitumineuse Épaisse [3.1.12, 8.3] :** Roulement/Base en BB (excl. EME) ; Fondation en BB ou GNT ; Ratio BB/total chaussée [0.45 ; 0.60].
*   **Semi-Rigide [3.1.13, 8.5] :** Roulement en BB ; Assise (Base/Fondation) en MTLH ; Min surface 0.06m ; Fondation min 0.15-0.20m par PF.
*   **Rigide (Béton) [3.1.16, 8.8] :** ≥1 couche Béton ≥0.12m ; kd intègre effets thermiques ; Interfaces spécifiques (voir 2.4).

*   **Vérifications Communes [8.1] :** Déformation εz sur plate-forme < εz adm (Éq. 28/29) ; Contraintes en fatigue pour liés.

---

#### **3. Missions Atomiques pour le Technicien (Actions Uniques et Localisées)**

Chaque mission est une tâche isolée, avec fichier/ligne cible. La priorité est donnée aux missions des lots 2.x et 4.x.

##### **Mission 2.1 : Restriction des Matériaux pour Plate-forme**

*   **Fichier :** LayerRowVM.cs (propriété AvailableMaterials)
*   **Action :** Si Layer.Role == Plateforme, retourner new[] { MaterialFamily.GNT, MaterialFamily.Bibliotheque } ; Sinon, liste complète.

##### **Mission 2.2 : Ajout de Validation pour Module E par Matériau**

*   **Fichier :** Layer.cs (méthode Validate())
*   **Action :** Ajouter switch sur Family ; Si hors plage (ex. GNT: if(Modulus_MPa < 100 || >1000)), set à borne proche et ajouter erreur "Module hors norme NF P98-086".

##### **Mission 2.3 : Validation Poisson Fixe avec Auto-Correction**

*   **Fichier :** Layer.cs (setter de Poisson)
*   **Action :** Si Family == GNT || BB, forcer Poisson = 0.35 si différent ; Pour MTLH/BC=0.25 ; Notifier via OnErrorsChanged.

##### **Mission 2.4 : Validation Épaisseur Min/Max avec Ajustement**

*   **Fichier :** Layer.cs (setter de Thickness_m)
*   **Action :** Switch sur Family et Role ; Ex. pour GNT Fondation: if(<0.10 || >0.35), clamp à borne ; Ajouter erreur si ajusté ; Moduler min par PF (si implémenté).

##### **Mission 2.5 : Implémentation Toast pour Erreurs**

*   **Fichier :** StructureEditorViewModel.cs (LayerChanged)
*   **Action :** Si layer.HasErrors après Validate(), appeler une méthode ShowToast("Valeur ajustée selon NF P98-086 : [détail]") utilisant Windows.UI.Notifications ou custom UserControl.

##### **Mission 3.1 : Validation sur Touche Entrée**

*   **Fichier :** StructureEditorView.xaml.cs (Numeric_PreviewKeyDown)
*   **Action :** Si e.Key == Key.Enter, appeler ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource() pour forcer validation.

##### **Mission 4.1 : Vérification Interfaces Automatique**

*   **Fichier :** StructureEditorViewModel.cs (méthode ValidateNow())
*   **Action :** Pour chaque interface, switch sur matériaux adjacents ; Forcer type (ex. Fondation-Plateforme = Collee) ; Si mismatch, set à valeur normative et notifier.

##### **Mission 4.2 : Vérification Structure Globale - Épaisseurs Totales**

*   **Fichier :** StructureEditorViewModel.cs (ValidateNow())
*   **Action :** Calculer total BB/GNT ; Si Type=Souple et BB>0.12 ou GNT<0.15, erreur bloquante MessageBox("Incohérent avec Souple [NF 3.1.11]").

##### **Mission 4.3 : Vérification Structure Globale - Présences Matériaux**

*   **Fichier :** StructureEditorViewModel.cs (ValidateNow())
*   **Action :** Si Type=SemiRigide et !Assise.Has(MTLH), erreur "Assise doit être MTLH [NF 3.1.13]".

##### **Mission 4.4 : Intégration Coefficients ks/kd**

*   **Fichier :** Layer.cs (nouvelle propriété)
*   **Action :** Ajouter calcul ks basé sur PF classe (ex. PF1=0.9) ; Utiliser dans futures simulations (non bloquant pour saisie).

##### **Mission 1.1 : Diagnostic Avancé du Rognage Visuel**

*   **Fichier :** StructureEditorView.xaml.cs
*   **Action :** Ajouter une méthode de debug : Utiliser VisualTreeHelper.GetDescendantBounds() sur le Border de la couche (dans Loaded event) pour logger les bounds réels de l'étiquette vs. le conteneur. Vérifier si un RenderTransform ou Padding implicite cause le clip.

##### **Mission 1.2 : Ajustement du Positionnement de l'Étiquette**

*   **Fichier :** StructureEditorView.xaml (ligne ~400, dans DataTemplate de ItemsControl pour Layers)
*   **Action :** Changer le Grid parent en Canvas ; Positionner l'étiquette avec Canvas.Bottom="-12" et Canvas.Left="100" (ajuster pour centrage) ; Supprimer Margin négatif pour éviter conflits.

##### **Mission 1.3 : Désactivation Globale du Clipping**

*   **Fichier :** StructureEditorView.xaml (ligne ~350, Border Grid.Column="1")
*   **Action :** Ajouter ClipToBounds="False" au DockPanel enfant du Border ; Tester en runtime si les étiquettes débordent sans rognage.

##### **Mission 1.4 : Alternative Popup pour Étiquettes**

*   **Fichier :** StructureEditorView.xaml (dans DataTemplate)
*   **Action :** Remplacer le Border de l'étiquette par un <Popup Placement="Bottom" AllowsTransparency="True" StaysOpen="True"> avec le TextBlock ; Binder IsOpen à une propriété VM toujours true pour couches non-Plateforme.
---

Ce rapport exhaustif et atomique fournit une feuille de route précise pour implémenter les règles complètes de NF P98-086, résoudre le bug visuel et améliorer l'ergonomie. Tester chaque mission isolément avant intégration.
