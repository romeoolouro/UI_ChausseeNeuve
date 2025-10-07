using System.Windows.Documents;

namespace UI_ChausseeNeuve.Reports;

/// <summary>
/// Contrat d'une section de la note de calcul. Chaque section est autonome et
/// peut �tre activ�e/d�sactiv�e / r�ordonn�e dynamiquement dans l'UI.
/// </summary>
public interface IReportSection
{
    /// <summary>Identifiant technique stable (ex: "summary", "inputs.traffic").</summary>
    string Id { get; }
    /// <summary>Titre affich� dans la note (num�rotation g�r�e � l'ext�rieur).</summary>
    string Title { get; }
    /// <summary>Ordre d'affichage (modifiable par l'utilisateur).</summary>
    int Order { get; set; }
    /// <summary>Inclusion dans le document final.</summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Construit le contenu FlowDocument (blocs) de la section.
    /// La section renvoie un FlowDocument temporaire dont les Blocks seront fusionn�s.
    /// </summary>
    FlowDocument Build(ReportContext context);
}
