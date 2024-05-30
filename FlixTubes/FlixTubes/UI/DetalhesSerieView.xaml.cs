using FlixTubes.Helpers;
using FlixTubes.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlixTubes.UI
{
    /// <summary>
    /// Interação lógica para DetalhesSerieView.xam
    /// </summary>
    public partial class DetalhesSerieView : System.Windows.Controls.UserControl
    {
        public event EventHandler? FecharHandler;
        private BoxArquivoView? _boxSerieSelecionado;
        private InfosSerie? _infosSerie;

        private List<DirectoryInfo>? ListaDirTemporadas;
        private List<FileInfo>? ListaArquivosTemporada;

        public DetalhesSerieView()
        {
            InitializeComponent();
        }

        private void btnFechar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _boxSerieSelecionado = null;
            FecharHandler?.Invoke(this, EventArgs.Empty);

        }

        public void CarregarDados(BoxArquivoView boxArquivoView)
        {
            _boxSerieSelecionado = boxArquivoView;

            if (_boxSerieSelecionado == null)
            {
                System.Windows.MessageBox.Show("Erro ao carregar filme!");
                btnFechar_MouseLeftButtonDown(null, null);
                return;
            }

            string dirSerie = _boxSerieSelecionado.DiretorioSerie.FullName ?? "";
            string NomeSerie = _boxSerieSelecionado.DiretorioSerie.Name;
            string uriImagem = "pack://application:,,,/Resources/box/capa.jpg";

            _infosSerie = InfosServicos.ReadFromJsonFile<InfosSerie>(dirSerie, NomeSerie) ?? new InfosSerie();

            txtTitulo.Text = _infosSerie.Nome ?? NomeSerie;
            txtTitulo.Visibility = Visibility.Visible;
            txtDescricao.Text = _infosSerie.Sinopse;

            //Carrega a imagem
            FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(dirSerie, NomeSerie + ".jpg"));
            if (fileInfo.Exists)
            {
                //Criar a URI da imagem com o identificador único
                uriImagem = "file:///" + fileInfo.FullName.Replace("\\", "/") + "?" + DateTime.Now.Ticks;
            }

            FuncoesUteis.CarregarImagemNoGrid(grdImagem, uriImagem, Stretch.UniformToFill);

            CarregarTemporadas();
        }


        public void CarregarTemporadas()
        {
            if (_boxSerieSelecionado == null) return;
            cbxTemporada.Items.Clear(); //Limpa a lista de itens
            ListaDirTemporadas = new List<DirectoryInfo>();


            var listaDiretorios = Directory.GetDirectories(_boxSerieSelecionado.DiretorioSerie.FullName, "*", SearchOption.TopDirectoryOnly).OrderBy(o => o).ToArray();

            foreach (var item in listaDiretorios)
            {
                ListaDirTemporadas.Add(new DirectoryInfo(item));
            }

            if (ListaDirTemporadas.Any())
            {
                cbxTemporada.ItemsSource = ListaDirTemporadas;
                cbxTemporada.SelectedIndex = 0;
            }
        }

        private void cbxTemporada_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CarregarLista();
        }

        public void CarregarLista()
        {
            if (lstEpisodios == null) return; 

            lstEpisodios.ItemsSource = null;
            ListaArquivosTemporada = new List<FileInfo>();

            DirectoryInfo? dirSelecionado = cbxTemporada.SelectedValue as DirectoryInfo;

            if (dirSelecionado == null) return;

            string[] arquivosTemporada = Directory.GetFiles(dirSelecionado.FullName, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp4") || s.EndsWith(".avi") || s.EndsWith(".mkv")).OrderBy(o => o).ToArray();

            foreach (var item in arquivosTemporada)
            {
                ListaArquivosTemporada.Add(new FileInfo(item));
            }

            lstEpisodios.ItemsSource = ListaArquivosTemporada;
        }

        private void lstEpisodios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Obtenha o arquivo selecionado
            FileInfo? selectedFile = lstEpisodios.SelectedItem as FileInfo;

            if (selectedFile != null)
            {
                // Abrir o arquivo com o programa associado
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedFile.FullName,
                    UseShellExecute = true,
                });
            }
        }

    }
}
