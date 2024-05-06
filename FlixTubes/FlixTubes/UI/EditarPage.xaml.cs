using FlixTubes.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
using System.Xml;

namespace FlixTubes.UI
{
    public partial class EditarPage : System.Windows.Controls.UserControl
    {
        public event EventHandler? CancelarHandler;
        public event EventHandler? SalvarHandler;

        public BoxArquivoView? _filmeSelecionado;

        private string? _dirImagemSelecionada;

        public EditarPage()
        {
            InitializeComponent();
        }

        #region Acoes

        private void grdImg_Drop(object sender, System.Windows.DragEventArgs e)
        {
            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

            if (files == null) return;

            string? primeiroArquivo = files.Where(s => s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".png")).FirstOrDefault();

            if (string.IsNullOrEmpty(primeiroArquivo))
            {
                System.Windows.MessageBox.Show("Formato inválido!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FileInfo fileInfo = new FileInfo(primeiroArquivo);
            if (fileInfo.Exists)
            {
                _dirImagemSelecionada = fileInfo.FullName;
                CarregarImagemNoGrid(fileInfo.FullName);
            }
            else
            {
                System.Windows.MessageBox.Show("não foi possivel carregar a imagem", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();

            op.Title = "Selecione a imagem";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";

            DialogResult result = op.ShowDialog();

            if (result == DialogResult.OK)
            {
                FileInfo fileInfo = new FileInfo(op.FileName);
                if (fileInfo.Exists)
                {
                    _dirImagemSelecionada = fileInfo.FullName;
                    //Carrega imagem
                    CarregarImagemNoGrid(fileInfo.FullName);
                }
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs? e)
        {
            _filmeSelecionado = null;
            CancelarHandler?.Invoke(this, e);
        }

        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            SalvarDados();
            SalvarHandler?.Invoke(this, e);
        }

        private void btnPesquisarCapa_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PesquisaCapaWeb();
        }

        #endregion

        #region Funcoes

        public void CarregarDados()
        {
            if (_filmeSelecionado == null || _filmeSelecionado.FileInfo == null)
            {
                btnCancelar_Click(btnCancelar, null);// fecha tudo
                return;
            }

            _dirImagemSelecionada = null;

            string dirArqFilme = _filmeSelecionado.FileInfo.DirectoryName ?? "";
            string NomeArqFilme = System.IO.Path.GetFileNameWithoutExtension(_filmeSelecionado.FileInfo.Name);

            //Carrega nome Arquivo
            txbArquivo.Text = NomeArqFilme;

            //Carrega a imagem
            FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(dirArqFilme, NomeArqFilme + ".jpg")); //Mudar!
            if (fileInfo.Exists)
            {
                // Criar a URI da imagem com o identificador único
                string uriImagem = "file:///" + fileInfo.FullName.Replace("\\", "/") + "?" + DateTime.Now.Ticks;
                CarregarImagemNoGrid(uriImagem);
            }
            else
            {
                CarregarImagemNoGrid("pack://application:,,,/Resources/box/capa.jpg");
            }

            //Carrega os infos
            InfosFilme infosFilme = InfosServicos.ReadFromJsonFile(dirArqFilme, NomeArqFilme) ?? new InfosFilme();

            txbNome.Text = infosFilme.Nome;
            txbSinopse.Text = infosFilme.Sinopse;
            txbTrailer.Text = infosFilme.IDVideoYoutube;
        }

        private void CarregarImagemNoGrid(string dirImagem)
        {
            // Carregar imagem e atribuir ao controle Image
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(dirImagem);
            bitmap.EndInit();
            grdImg.Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
        }

        private void PesquisaCapaWeb()
        {
            if (_filmeSelecionado == null || _filmeSelecionado.FileInfo == null) return;

            //da prioridade pro nome do filme mas se for nulo pesquisa pelo nome do arquivo
            string termoPesquisa = $"Capa {txbNome.Text ?? System.IO.Path.GetFileNameWithoutExtension(_filmeSelecionado.FileInfo.Name)}";

            // Construa a URL de pesquisa do Google Imagens com o termo de pesquisa
            string urlPesquisa = $"https://www.google.com/search?q={termoPesquisa}&tbm=isch";

            // Abra a URL no navegador padrão
            Process.Start(new ProcessStartInfo
            {
                FileName = urlPesquisa,
                UseShellExecute = true
            });
        }

        private void SalvarDados()
        {
            if (_filmeSelecionado == null || _filmeSelecionado.FileInfo == null) return;

            if (string.IsNullOrEmpty(txbArquivo.Text.Replace(" ", "")))
            {
                System.Windows.MessageBox.Show("Nome arquivo precisa ser preenchido!");
                return;
            }

            FileInfo fileAtual = _filmeSelecionado.FileInfo;
            bool alterouNomeArquivo = false;

            //Altera o Nome do Arquivo caso nao for igual
            if (txbArquivo.Text != System.IO.Path.GetFileNameWithoutExtension(fileAtual.Name))
            {
                string extensao = System.IO.Path.GetExtension(fileAtual.FullName);
                string nome = txbArquivo.Text;
                string diretorio = fileAtual.DirectoryName ?? "";

                File.Move(fileAtual.FullName, System.IO.Path.Combine(diretorio, nome + extensao));
                fileAtual = new FileInfo(System.IO.Path.Combine(diretorio, nome + extensao));

                alterouNomeArquivo = true;
            }

            //Salva nova imagem
            if (File.Exists(_dirImagemSelecionada)) //Se selecionou uma nova imagem
            {
                string nome = System.IO.Path.GetFileNameWithoutExtension(fileAtual.Name);
                string diretorio = fileAtual.DirectoryName ?? "";
                File.Copy(_dirImagemSelecionada, System.IO.Path.Combine(diretorio, nome + ".jpg"), true); //sobreescreve se já estiver la
            }

            //Salva as infos
            string nomeJson = System.IO.Path.GetFileNameWithoutExtension(fileAtual.Name);
            string diretorioJson = fileAtual.DirectoryName ?? "";
            InfosServicos.SaveToJsonFile(diretorioJson, nomeJson, new InfosFilme()
            {
                Nome = txbNome.Text,
                IDVideoYoutube = txbTrailer.Text,
                Sinopse = txbSinopse.Text
            });

            //Se teve mudanca no nome do arquivo
            if (alterouNomeArquivo)
            {
                string nomeOld = System.IO.Path.GetFileNameWithoutExtension(_filmeSelecionado.FileInfo.Name);
                string diretorio = fileAtual.DirectoryName ?? "";

                //Verifica a imagem 
                if (File.Exists(System.IO.Path.Combine(diretorio, nomeOld + ".jpg")))
                {
                    if (File.Exists(_dirImagemSelecionada)) //se carregou nova deleta
                    {
                        File.Delete(System.IO.Path.Combine(diretorio, nomeOld + ".jpg"));
                    }
                    else //se nao renomeia
                    {
                        string nomeNew = System.IO.Path.GetFileNameWithoutExtension(fileAtual.Name);
                        File.Move(System.IO.Path.Combine(diretorio, nomeOld + ".jpg"), System.IO.Path.Combine(diretorio, nomeNew + ".jpg"), true);
                    }
                }

                //Deleta as infos com nome anterior
                File.Delete(System.IO.Path.Combine(diretorio, nomeOld + ".json"));
            }
        }

        #endregion

    }
}
