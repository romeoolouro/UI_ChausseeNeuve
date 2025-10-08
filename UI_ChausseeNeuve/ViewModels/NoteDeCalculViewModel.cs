using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using UI_ChausseeNeuve.Reports;
using ChausseeNeuve.Domain.Models;
using Microsoft.Win32;
using System.Windows;

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
            private set { _isBusy = value; OnPropertyChanged(); RefreshCommand.RaiseCanExecuteChanged(); ExportXpsCommand.RaiseCanExecuteChanged(); }
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
        public RelayCommand ExportXpsCommand { get; }

        public NoteDeCalculViewModel()
        {
            RefreshCommand = new RelayCommand(Generate, () => !IsBusy);
            ExportXpsCommand = new RelayCommand(ExportXps, () => Document != null && !IsBusy);
            Generate();
        }

        private void Generate()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Status = "Génération...";

                // IMPORTANT : construire le FlowDocument UNIQUEMENT sur le thread UI
                var project = AppState.CurrentProject;
                if (project?.PavementStructure == null)
                {
                    Status = "Projet/structure invalide";
                    Document = CreateErrorDocument("Structure absente");
                    return;
                }

                // Utiliser le générateur (qui prend maintenant un snapshot interne) – aucune énumération live
                var doc = NoteDeCalculGenerator.Generate(project, null, "Normal", CreateFingerprint(project), includeDebug: ShowDebug);
                Document = doc;
                Status = "Note générée";
            }
            catch (Exception ex)
            {
                Status = "Erreur: " + ex.Message;
                Document = CreateErrorDocument(ex.Message);
                System.Diagnostics.Debug.WriteLine("NoteDeCalcul - Exception: " + ex);
            }
            finally
            {
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
            fd.Blocks.Add(new Paragraph(new Bold(new Run("Erreur de génération"))) { Foreground = System.Windows.Media.Brushes.Red, FontSize = 18 });
            fd.Blocks.Add(new Paragraph(new Run("Détails: " + msg)) { Margin = new Thickness(0,8,0,0) });
            return fd;
        }

        private void ExportXps()
        {
            try
            {
                if (Document == null) { Status = "Aucun document"; return; }
                var dlg = new SaveFileDialog
                {
                    Filter = "Document XPS (*.xps)|*.xps",
                    FileName = $"NoteCalcul_{DateTime.Now:yyyyMMdd_HHmm}.xps"
                };
                if (dlg.ShowDialog() != true) return;
                using var xpsDoc = new System.Windows.Xps.Packaging.XpsDocument(dlg.FileName, System.IO.FileAccess.ReadWrite);
                var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                if (Document is IDocumentPaginatorSource dps)
                    writer.Write(dps.DocumentPaginator);
                Status = "Export XPS OK";
            }
            catch (Exception ex)
            {
                Status = "Erreur export: " + ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
