using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using UI_ChausseeNeuve.Reports;
using ChausseeNeuve.Domain.Models;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RunOpenXml = DocumentFormat.OpenXml.Wordprocessing.Run;
using ParagraphOpenXml = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace UI_ChausseeNeuve.ViewModels
{
    public class NoteDeCalculViewModel : INotifyPropertyChanged
    {
        private FlowDocument? _document;
        private bool _isBusy;
        private string _status = "Prêt";
        private bool _showDebug; // toggle UI futur

        public FlowDocument? Document
        {
            get => _document;
            private set { _document = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); RefreshCommand.RaiseCanExecuteChanged(); ExportPdfCommand.RaiseCanExecuteChanged(); ExportWordCommand.RaiseCanExecuteChanged(); }
        }

        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }

        public bool ShowDebug
        {
            get => _showDebug;
            set { _showDebug = value; OnPropertyChanged(); Generate(); }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ExportPdfCommand { get; }
        public RelayCommand ExportWordCommand { get; }

        // ================= Bandeau Mode Dimensionnement =================
        public string StructureType => AppState.CurrentProject?.PavementStructure?.StructureType ?? "Structure n/d";

        public string ModeDimensionnement
        {
            get
            {
                var modeStr = AppState.CurrentProject?.Mode switch
                {
                    DimensionnementMode.Automatique => "MODE AUTOMATIQUE",
                    DimensionnementMode.Expert => "MODE EXPERT",
                    _ => "MODE INCONNU"
                };
                var structStr = StructureType;
                return $"{modeStr}  •  {structStr.ToUpperInvariant()}";
            }
        }

        public string ModeDescription => AppState.CurrentProject?.Mode switch
        {
            DimensionnementMode.Automatique => "Paramètres & coefficients issus des règles et bibliothèques.",
            DimensionnementMode.Expert => "Valeurs ajustées manuellement (responsabilité utilisateur).",
            _ => "Projet sans mode défini."
        };

        public string ModeBannerColor => AppState.CurrentProject?.Mode switch
        {
            DimensionnementMode.Automatique => "#2F80ED",
            DimensionnementMode.Expert => "#C0392B",
            _ => "#616161"
        };

        // Nouveau : statut global validation (toutes valeurs admissibles)
        public string ValidationGlobalText
        {
            get
            {
                var res = AppState.CurrentProject?.DerniersResultats;
                if (res == null || res.Count == 0) return "VALIDATION: NC";
                bool allValid = res.All(r => r.EstValide || !r.SoupleEpsiZApplicable);
                return allValid ? "VALIDATION: OK" : "VALIDATION: NON CONFORME";
            }
        }
        public string ValidationGlobalColor
        {
            get
            {
                var t = ValidationGlobalText;
                if (t.Contains("OK")) return "#27AE60"; // vert
                if (t.Contains("NON")) return "#E74C3C"; // rouge
                return "#F1C40F"; // jaune / neutre
            }
        }

        private void RaiseModeProperties()
        {
            OnPropertyChanged(nameof(ModeDimensionnement));
            OnPropertyChanged(nameof(ModeDescription));
            OnPropertyChanged(nameof(ModeBannerColor));
            OnPropertyChanged(nameof(StructureType));
            OnPropertyChanged(nameof(ValidationGlobalText));
            OnPropertyChanged(nameof(ValidationGlobalColor));
        }

        public NoteDeCalculViewModel()
        {
            RefreshCommand = new RelayCommand(Generate, () => !IsBusy);
            ExportPdfCommand = new RelayCommand(ExportPdf, () => Document != null && !IsBusy);
            ExportWordCommand = new RelayCommand(ExportWord, () => Document != null && !IsBusy);
            try { AppState.StructureChanged += OnStructureChanged; } catch { }
            Generate();
        }

        private void OnStructureChanged() => Application.Current?.Dispatcher.BeginInvoke(new Action(RaiseModeProperties));

        private void Generate()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Status = "Génération...";
                var project = AppState.CurrentProject;
                if (project?.PavementStructure == null)
                {
                    Status = "Projet/structure invalide";
                    Document = CreateErrorDocument("Structure absente");
                    return;
                }
                var doc = NoteDeCalculGenerator.Generate(project, null, "Normal", CreateFingerprint(project), includeDebug: ShowDebug);
                Document = doc;
                Status = "Note générée";
            }
            catch (Exception ex)
            {
                Status = "Erreur: " + ex.Message;
                Document = CreateErrorDocument(ex.Message);
            }
            finally
            {
                RaiseModeProperties();
                IsBusy = false;
            }
        }

        private string CreateFingerprint(Project p)
        {
            try
            {
                var raw = $"{p.Name}|{p.Mode}|{p.PavementStructure.StructureType}|{p.PavementStructure.NE}";
                return raw.GetHashCode().ToString("X");
            }
            catch { return "NA"; }
        }

        private FlowDocument CreateErrorDocument(string msg)
        {
            var fd = new FlowDocument();
            fd.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Erreur de génération"))) { Foreground = System.Windows.Media.Brushes.Red, FontSize = 18 });
            fd.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Détails: " + msg)) { Margin = new Thickness(0,8,0,0) });
            return fd;
        }

        private void ExportPdf()
        {
            try
            {
                if (Document == null) { Status = "Aucun document"; return; }
                var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"NoteCalcul_{DateTime.Now:yyyyMMdd_HHmm}.pdf" };
                if (dlg.ShowDialog() != true) return;

                using var pdf = new PdfDocument();
                var font = new XFont("Arial", 10);
                foreach (var block in Document.Blocks)
                {
                    var page = pdf.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    var gfx = XGraphics.FromPdfPage(page);
                    string text = new TextRange(block.ContentStart, block.ContentEnd).Text;
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var lines = text.Replace("\r", "").Split('\n');
                    double y = 40;
                    foreach (var line in lines)
                    {
                        if (y > page.Height - 40) break; // simple overflow cut
                        gfx.DrawString(line, font, XBrushes.Black, new XPoint(40, y));
                        y += 14;
                    }
                }
                pdf.Save(dlg.FileName);
                Status = "PDF exporté";
            }
            catch (Exception ex)
            {
                Status = "Erreur PDF: " + ex.Message;
            }
        }

        private void ExportWord()
        {
            try
            {
                if (Document == null) { Status = "Aucun document"; return; }
                var dlg = new SaveFileDialog { Filter = "Word (*.docx)|*.docx", FileName = $"NoteCalcul_{DateTime.Now:yyyyMMdd_HHmm}.docx" };
                if (dlg.ShowDialog() != true) return;

                using var stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.ReadWrite);
                using var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();
                mainPart.Document.Append(body);

                foreach (var block in Document.Blocks)
                {
                    string text = new TextRange(block.ContentStart, block.ContentEnd).Text;
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var p = new ParagraphOpenXml();
                    p.Append(new RunOpenXml(new Text(text))); 
                    body.Append(p);
                }
                mainPart.Document.Save();
                Status = "DOCX exporté";
            }
            catch (Exception ex)
            {
                Status = "Erreur DOCX: " + ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
