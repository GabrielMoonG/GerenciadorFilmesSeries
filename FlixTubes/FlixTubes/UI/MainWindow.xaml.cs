using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
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

        string? _dirSelecionado;

        string[]? ListaArquivos;
        string[]? ListaFiltrado;

        Areas _areaAtual;

        enum Areas
        {
            Configuracoes,
            Filmes,
        }

        private CancellationTokenSource? cancellationTokenSource; //Usado para cancelar a Task

        #endregion

        #region Window

        public MainWindow()
        {
            InitializeComponent();

            formWindow.CancelarHandler += FecharEditar_Handler;
            formWindow.SalvarHandler += FecharEditar_Handler;


            _dirSelecionado = BuscarDirNoRegistro("FILMES");

            if (!string.IsNullOrEmpty(_dirSelecionado))
            {
                CarregarListaFilmes();
            }


            LinkMenu_MouseLeftButtonDown(btnFilmes, null);

            grdDetalhes.Visibility = Visibility.Collapsed;
        }


        #endregion

        #region Area Filmes

        //---- Acoes ----//

        private void txbPesquisar_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CarregarListaFilmes();
        }

        private void imgRecarregarFilmes_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CarregarListaFilmes();
        }

        //---- Funcoes ----//

        public async void ProcessarArquivos()
        {
            // Cancela a operação anterior, se houver
            cancellationTokenSource?.Cancel();

            // Cria um novo CancellationTokenSource para a nova operação
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Inicia a operação em uma thread separada
            await Task.Run(() => LoadBoxArquivoView(cancellationToken), cancellationToken);
        }

        private void LoadBoxArquivoView(CancellationToken cancellationToken)
        {
            Dispatcher.Invoke(() =>
            {
                panelFilmes.Children.Clear(); //Limpa tudo

                if (ListaArquivos?.Length != ListaFiltrado?.Length)
                {
                    txbQtdFilmes.Text = $"{ListaFiltrado?.Length} de {ListaArquivos?.Length}";
                }
                else
                {
                    txbQtdFilmes.Text = $"{ListaArquivos?.Length}";
                }
            });

            if (ListaFiltrado == null || ListaFiltrado.Length == 0) return;

            foreach (var item in ListaFiltrado)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                FileInfo fileInfo = new FileInfo(item);
                Dispatcher.Invoke(() =>
                {
                    BoxArquivoView novoBox = new BoxArquivoView(fileInfo);
                    novoBox.EditarHandler += AbrirEdicao_Handler;
                    novoBox.DetalhesHandler += AbrirDetalhes_Handler;
                    panelFilmes.Children.Add(novoBox);//adiciona o filme
                });

                Thread.Sleep(10);
            }
        }

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

        #endregion

        #region Menu Superior 

        //---- Acoes ----//

        private void LinkMenu_MouseLeftButtonDown(object sender, MouseButtonEventArgs? e)
        {
            var click = sender as FrameworkElement;

            if (click == null) return;

            switch (click.Name)
            {
                case "btnFilmes":
                    ExibirArea(Areas.Filmes);
                    break;
                case "btnConfig":
                    ExibirArea(Areas.Configuracoes);
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
                    break;
                case Areas.Filmes:
                    grdFilmes.Visibility = Visibility.Visible;
                    btnFilmes.Opacity = 1;
                    break;
            }
        }

        private void OcultarAreas()
        {
            grdFilmes.Visibility = Visibility.Hidden; btnFilmes.Opacity = 0.3;
            grdConfig.Visibility = Visibility.Hidden; btnConfig.Opacity = 0.3;
        }

        #endregion

        #region Configuracoes

        //---- Acoes ----//

        private void btnConfigFilme_Click(object sender, RoutedEventArgs e)
        {
            AbrirSelecaoDirFilmes();
        }

        //---- Funcoes ----//

        private void AbrirSelecaoDirFilmes()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();

            //---- Se selecionou um diretorio ----//
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                _dirSelecionado = folderBrowserDialog.SelectedPath;

                RegistrarDirNoRegistro("FILMES", _dirSelecionado);//Salvar no Registro               
                CarregarListaFilmes();
                txbPesquisar.Text = string.Empty; //limpa a pesquisa do filmes
                LinkMenu_MouseLeftButtonDown(btnFilmes, null); //manda para area de filmes
            }
        }

        private void CarregarListaFilmes()
        {
            if (string.IsNullOrEmpty(_dirSelecionado)) return;

            lblDirFilme.Text = _dirSelecionado; //Exibe nas configs o dirSelecionado

            //pega a lista de arquivos dentra da pasta co
            ListaArquivos = Directory.GetFiles(_dirSelecionado, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp4") || s.EndsWith(".avi") || s.EndsWith(".mkv")).OrderBy(o => o).ToArray();
            ListaFiltrado = ListaArquivos;

            if (!string.IsNullOrEmpty(txbPesquisar.Text))
            {
                ListaFiltrado = ListaArquivos.Where(o => FormataTextoPraPesquisa(o.ToLower()).Contains(FormataTextoPraPesquisa(txbPesquisar.Text.ToLower()))).ToArray();
            }

            ProcessarArquivos();
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

        #region Edição

        private void FecharEditar_Handler(object? sender, EventArgs e)
        {
            grdEditar.Visibility = Visibility.Collapsed;
        }

        private void FecharDetalhes_Handler(object? sender, EventArgs e)
        {
            grdDetalhes.Visibility = Visibility.Collapsed;
            grdDetalhes.Children.Clear();
        }

        private void AbrirEdicao_Handler(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_dirSelecionado)) return;

            BoxArquivoView? box = sender as BoxArquivoView;

            if (box == null) return;

            if (box != null)
            {
                grdEditar.Visibility = Visibility.Visible;
                formWindow._diretorioFilmes = _dirSelecionado;
                formWindow.BoxSelecionado = box;
                formWindow.CarregarDados();
            }
        }

        private void AbrirDetalhes_Handler(object? sender, EventArgs e)
        {
            BoxArquivoView? box = sender as BoxArquivoView;

            if (box == null) return;

            if (box != null)
            {
                grdDetalhes.Visibility = Visibility.Visible;
                DetalhesPage detalhesPage = new DetalhesPage();
                detalhesPage._boxFilmeSelecionado = box;
                detalhesPage.CarregarDados(); detalhesPage.Margin = new Thickness(50);
                detalhesPage.FecharHandler += FecharDetalhes_Handler;
                grdDetalhes.Children.Add(detalhesPage);

            }
        }

        #endregion
    }
}