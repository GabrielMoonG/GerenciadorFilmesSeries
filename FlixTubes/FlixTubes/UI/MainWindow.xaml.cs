using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlixTubes.UI
{
    public partial class MainWindow : System.Windows.Window
    {
        #region Variáveis

        string? _dirFilmes;
        string? _dirSeries;

        string[]? ListaFilmes;
        string[]? ListaFilmesFiltrado;

        string[]? ListaSeries;
        string[]? ListaSeriesFiltrado;

        Areas _areaAtual;

        enum Areas
        {
            Configuracoes,
            Filmes,
            Series,
        }

        private CancellationTokenSource? cancellationTokenSourceFilmes; //Usado para cancelar a Task
        private CancellationTokenSource? cancellationTokenSourceSeries; //Usado para cancelar a Task

        #endregion

        #region Window

        public MainWindow()
        {
            InitializeComponent();

            formWindow.CancelarHandler += FecharFormulario_Handler;
            formWindow.SalvarHandler += SalvouFormulario_Handler;


            _dirFilmes = BuscarDirNoRegistro("FILMES");

            LinkMenu_MouseLeftButtonDown(btnFilmes, null);

            if (!string.IsNullOrEmpty(_dirFilmes))
            {
                CarregarListaFilmes();
            }

            _dirSeries = BuscarDirNoRegistro("SERIES");
            if (!string.IsNullOrEmpty(_dirSeries))
            {
                CarregarListaSeries();
            }

            grdDetalhes.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Area Filmes

        //---- Acoes ----//

        private void txbPesquisarFilme_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CarregarListaFilmes();
        }

        private void imgRecarregarFilmes_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CarregarListaFilmes();
        }

        //---- Funcoes ----//

        private void CarregarListaFilmes()
        {
            if (string.IsNullOrEmpty(_dirFilmes)) return;

            lblDirFilme.Text = _dirFilmes; //Exibe nas configs o dirSelecionado

            //pega a lista de arquivos dentra da pasta co
            ListaFilmes = Directory.GetFiles(_dirFilmes, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp4") || s.EndsWith(".avi") || s.EndsWith(".mkv")).OrderBy(o => o).ToArray();
            ListaFilmesFiltrado = ListaFilmes;

            if (!string.IsNullOrEmpty(txbPesquisarFilme.Text))
            {
                ListaFilmesFiltrado = ListaFilmes.Where(o => FormataTextoPraPesquisa(o.ToLower()).Contains(FormataTextoPraPesquisa(txbPesquisarFilme.Text.ToLower()))).ToArray();
            }

            ProcessarFilmes();
        }

        public async void ProcessarFilmes()
        {
            // Cancela a operação anterior, se houver
            cancellationTokenSourceFilmes?.Cancel();

            // Cria um novo CancellationTokenSource para a nova operação
            cancellationTokenSourceFilmes = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSourceFilmes.Token;

            // Inicia a operação em uma thread separada
            await Task.Run(() => LoadBoxFilmesView(cancellationToken), cancellationToken);
        }

        private void LoadBoxFilmesView(CancellationToken cancellationToken)
        {
            Dispatcher.Invoke(() =>
            {
                panelFilmes.Children.Clear(); //Limpa tudo

                if (ListaFilmes?.Length != ListaFilmesFiltrado?.Length)
                {
                    txbQtdFilmes.Text = $"{ListaFilmesFiltrado?.Length} de {ListaFilmes?.Length}";
                }
                else
                {
                    txbQtdFilmes.Text = $"{ListaFilmes?.Length}";
                }
            });

            if (ListaFilmesFiltrado == null || ListaFilmesFiltrado.Length == 0) return;

            foreach (var item in ListaFilmesFiltrado)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                FileInfo fileInfo = new FileInfo(item);
                Dispatcher.Invoke(() =>
                {
                    BoxArquivoView novoBox = new BoxArquivoView(fileInfo);
                    novoBox.EditarHandler += AbrirFormulario_Handler;
                    novoBox.DetalhesHandler += AbrirDetalhesFilme_Handler;
                    panelFilmes.Children.Add(novoBox);//adiciona o filme
                });

                Thread.Sleep(10);
            }
        }

        #endregion

        #region Area Series

        private void txbPesquisarSerie_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CarregarListaSeries();
        }

        private void imgRecarregarSerie_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CarregarListaSeries();
        }

        private void CarregarListaSeries()
        {
            if (string.IsNullOrEmpty(_dirSeries)) return;

            lblDirSerie.Text = _dirSeries; //Exibe nas configs o dirSelecionado

            //pega a lista de arquivos dentra da pasta co
            ListaSeries = Directory.GetDirectories(_dirSeries, "*.*",SearchOption.TopDirectoryOnly).OrderBy(o => o).ToArray();
            ListaSeriesFiltrado = ListaSeries;

            if (!string.IsNullOrEmpty(txbPesquisarSerie.Text))
            {
                ListaSeriesFiltrado = ListaSeries.Where(o => FormataTextoPraPesquisa(o.ToLower()).Contains(FormataTextoPraPesquisa(txbPesquisarSerie.Text.ToLower()))).ToArray();
            }

            ProcessarSeries();
        }

        public async void ProcessarSeries()
        {
            // Cancela a operação anterior, se houver
            cancellationTokenSourceSeries?.Cancel();

            // Cria um novo CancellationTokenSource para a nova operação
            cancellationTokenSourceSeries = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSourceSeries.Token;

            // Inicia a operação em uma thread separada
            await Task.Run(() => LoadBoxSeriesView(cancellationToken), cancellationToken);
        }

        private void LoadBoxSeriesView(CancellationToken cancellationToken)
        {
            Dispatcher.Invoke(() =>
            {
                panelSeries.Children.Clear(); //Limpa tudo

                if (ListaSeries?.Length != ListaSeriesFiltrado?.Length)
                {
                    txbQtdFilmes.Text = $"{ListaSeriesFiltrado?.Length} de {ListaSeries?.Length}";
                }
                else
                {
                    txbQtdSeries.Text = $"{ListaSeries?.Length}";
                }
            });

            if (ListaSeriesFiltrado == null || ListaSeriesFiltrado.Length == 0) return;

            foreach (var item in ListaSeriesFiltrado)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                DirectoryInfo directoryInfo = new DirectoryInfo(item);
                Dispatcher.Invoke(() =>
                {
                    BoxArquivoView novoBox = new BoxArquivoView(directoryInfo);
                    //novoBox.EditarHandler += AbrirFormulario_Handler;
                    novoBox.DetalhesHandler += AbrirDetalhesSerie_Handler;
                    panelSeries.Children.Add(novoBox);//adiciona a serie
                });

                Thread.Sleep(10);
            }
        }

        #endregion

        #region Detalhes Filme

        private void FecharDetalhes_Handler(object? sender, EventArgs e)
        {
            grdDetalhes.Visibility = Visibility.Collapsed;
            grdDetalhes.Children.Clear();
        }

        private void AbrirDetalhesFilme_Handler(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                grdDetalhes.Children.Clear();

                BoxArquivoView? box = sender as BoxArquivoView;

                if (box == null) return;

                if (box != null)
                {
                    DetalhesPage detalhesPage = new DetalhesPage();
                    detalhesPage.CarregarDados(box);
                    detalhesPage.Margin = new Thickness(50);
                    detalhesPage.FecharHandler += FecharDetalhes_Handler;

                    grdDetalhes.Children.Add(detalhesPage);
                    grdDetalhes.Visibility = Visibility.Visible;
                }
            });
        }
        private void AbrirDetalhesSerie_Handler(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                grdDetalhes.Children.Clear();

                BoxArquivoView? box = sender as BoxArquivoView;

                if (box == null) return;

                if (box != null)
                {
                    DetalhesSerieView detalhesPage = new DetalhesSerieView();
                    detalhesPage.CarregarDados(box);
                    detalhesPage.Margin = new Thickness(50);
                    detalhesPage.FecharHandler += FecharDetalhes_Handler;

                    grdDetalhes.Children.Add(detalhesPage);
                    grdDetalhes.Visibility = Visibility.Visible;
                }
            });
        }

        #endregion

        #region Formulário

        private void FecharFormulario_Handler(object? sender, EventArgs e)
        {
            grdFormulario.Visibility = Visibility.Collapsed;
        }

        private void AbrirFormulario_Handler(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_dirFilmes))
            {
                System.Windows.MessageBox.Show("Configure um diretório primeiro!");
                return;
            }

            //precisa selecionar um diretorio

            BoxArquivoView? box = sender as BoxArquivoView;

            formWindow.LimparVariaveis();
            formWindow._diretorioFilmes = _dirFilmes;

            if (box != null)
            {
                formWindow.BoxSelecionado = box;
                formWindow.CarregarDados(box.ArquivoFilme);
            }

            //Exibe o formulario
            grdFormulario.Visibility = Visibility.Visible;
        }

        private void SalvouFormulario_Handler(object? sender, EventArgs e)
        {
            FileInfo? fileInfo = sender as FileInfo;

            //É um novo adiciona na lista
            if (formWindow.BoxSelecionado == null && fileInfo != null)
            {
                BoxArquivoView novoBox = new BoxArquivoView(fileInfo);
                novoBox.EditarHandler += AbrirFormulario_Handler;
                novoBox.DetalhesHandler += AbrirDetalhesFilme_Handler;
                panelFilmes.Children.Add(novoBox);//adiciona o filme
            }

            grdFormulario.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Menu Superior 

        //---- Acoes ----//

        private void LinkMenu_MouseLeftButtonDown(object sender, MouseButtonEventArgs? e)
        {
            var click = sender as FrameworkElement;

            if (click == null) return;

            switch (click.Name)
            {
                case "btnConfig":
                    ExibirArea(Areas.Configuracoes);
                    break;
                case "btnFilmes":
                    ExibirArea(Areas.Filmes);
                    break;
                case "btnSeries":
                    ExibirArea(Areas.Series);
                    break;
            }
        }

        private void LinkMenu_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var botao = sender as FrameworkElement;

            if (botao == null) return;

            botao.Opacity = 1;
        }

        private void LinkMenu_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var botao = sender as FrameworkElement;

            if ((botao == null) ||
                (_areaAtual == Areas.Filmes && botao.Name == "btnFilmes") ||
                (_areaAtual == Areas.Series && botao.Name == "btnSeries") ||
                (_areaAtual == Areas.Configuracoes && botao.Name == "btnConfig"))
            {
                return;
            }

            botao.Opacity = 0.3;
        }

        //---- Funcoes ----//

        private void ExibirArea(Areas area)
        {
            OcultarAreas();

            _areaAtual = area;

            switch (_areaAtual)
            {
                case Areas.Configuracoes:
                    grdConfig.Visibility = Visibility.Visible;
                    btnConfig.Opacity = 1;
                    txbPesquisarFilme.Text = string.Empty; //limpa a pesquisa do filmes
                    break;
                case Areas.Filmes:
                    grdFilmes.Visibility = Visibility.Visible;
                    btnFilmes.Opacity = 1;
                    break;
                case Areas.Series:
                    grdSeries.Visibility = Visibility.Visible;
                    btnSeries.Opacity = 1;
                    txbPesquisarSerie.Text = string.Empty; //limpa a pesquisa da series
                    break;
            }
        }

        private void OcultarAreas()
        {
            grdFilmes.Visibility = Visibility.Hidden; btnFilmes.Opacity = 0.3;
            grdSeries.Visibility = Visibility.Hidden; btnSeries.Opacity = 0.3;
            grdConfig.Visibility = Visibility.Hidden; btnConfig.Opacity = 0.3;
        }

        #endregion

        #region Configuracoes

        //---- Acoes ----//

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button? btn = sender as System.Windows.Controls.Button;

            if (btn == null) return;

            switch (btn.Name)
            {
                case "btnConfigFilme":
                    AbrirSelecaoDirFilmes();
                    break;
                case "btnConfigSerie":
                    AbrirSelecaoDirSeries();
                    break;
            }

        }

        //---- Funcoes ----//

        private void AbrirSelecaoDirFilmes()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();

            //---- Se selecionou um diretorio ----//
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                _dirFilmes = folderBrowserDialog.SelectedPath;

                RegistrarDirNoRegistro("FILMES", _dirFilmes);//Salvar no Registro
                LinkMenu_MouseLeftButtonDown(btnFilmes, null); //manda para area de filmes
                CarregarListaFilmes();
            }
        }

        private void AbrirSelecaoDirSeries()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();

            //---- Se selecionou um diretorio ----//
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                _dirSeries = folderBrowserDialog.SelectedPath;

                RegistrarDirNoRegistro("SERIES", _dirSeries);//Salvar no Registro
                LinkMenu_MouseLeftButtonDown(btnSeries, null); //manda para area de series
                CarregarListaSeries();
            }
        }


        public static void RegistrarDirNoRegistro(string nomeDiretorio, string info)
        {
            Registry.CurrentUser.SetValue($"DIR_{nomeDiretorio}_FLIXTUBES", info);
        }

        public static string? BuscarDirNoRegistro(string nomeDiretorio)
        {
            return Registry.CurrentUser.GetValue($"DIR_{nomeDiretorio}_FLIXTUBES", "").ToString();
        }

        #endregion

        public string FormataTextoPraPesquisa(string input)
        {
            // Remove os acentos
            string normalizedString = input.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // Remove espaços em branco, hifens e underscores
            string result = Regex.Replace(stringBuilder.ToString(), @"[\s-_]", "");

            return result;
        }
    }
}