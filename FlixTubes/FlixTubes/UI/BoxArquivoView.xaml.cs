using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
using static FlixTubes.UI.EditarPage;

namespace FlixTubes.UI
{
    public partial class BoxArquivoView : System.Windows.Controls.UserControl
    {
        public FileInfo? FileInfo { get; set; }

        public event EventHandler? EditarHandler;
        public event EventHandler? DetalhesHandler;

        public BoxArquivoView()
        {
            InitializeComponent();
        }

        public BoxArquivoView(FileInfo file)
        {
            InitializeComponent();
            CarregarDados(file);
        }

        public void CarregarDados(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            txtNomeFilme.Text = FileInfo.Name;
            ProcurarCapa();
        }

        public void ProcurarCapa()
        {
            if (FileInfo == null || string.IsNullOrEmpty(FileInfo.DirectoryName)) return;
            // Suponha que fileInfo seja seu objeto FileInfo

            string nome = System.IO.Path.GetFileNameWithoutExtension(FileInfo.FullName);

            string caminhoImagem = System.IO.Path.Combine(FileInfo.DirectoryName, nome + ".jpg");

            // Criar a URI da imagem com o identificador único
            string uriImagem = "file:///" + caminhoImagem.Replace("\\", "/") + "?" + DateTime.Now.Ticks;


            // Verifique se o arquivo existe
            if (File.Exists(caminhoImagem))
            {
                grid.Background = null;

                // Carregar imagem e atribuir ao controle Image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Carrega a imagem diretamente do arquivo sem bloqueá-lo
                bitmap.UriSource = new Uri(uriImagem);
                bitmap.EndInit();
                grid.Background = new ImageBrush(bitmap){ Stretch = Stretch.UniformToFill };
            }
        }

        private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                PlayFilme();
            }
            else
            {
                DetalhesHandler?.Invoke(this, e);
            }
        }

        private void imgBtnExcluir_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FileInfo == null) return;

            var resposta = System.Windows.MessageBox.Show($"Deseja realmente excluir é irreversivel!! \r\n {FileInfo.Name}", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resposta == MessageBoxResult.Yes)
            {
                FileInfo.Delete();
                System.Windows.MessageBox.Show($"Deletado com Sucesso!", "Sucesso", MessageBoxButton.OK);

                // Obtenha o pai deste UserControl
                var parent = this.Parent as System.Windows.Controls.Panel;

                // Verifique se o pai é um Panel válido
                if (parent != null)
                {
                    // Remova este UserControl do pai
                    parent.Children.Remove(this);
                }
            }
        }

        private void imgBtnEditar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EditarHandler?.Invoke(this, e);
        }

        public void PlayFilme()
        {
            if (FileInfo == null) return;

            // Abrir o arquivo com o programa associado
            Process.Start(new ProcessStartInfo
            {
                FileName = FileInfo.FullName,
                UseShellExecute = true,
            });
        }
    }
}
