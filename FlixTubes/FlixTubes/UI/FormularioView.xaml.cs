using FlixTubes.Models;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlixTubes.UI
{
    public partial class FormularioView : System.Windows.Controls.UserControl
    {
        public event EventHandler? CancelarHandler; //evento quando cancela
        public event EventHandler? SalvarHandler; //evento quando salva

        public BoxArquivoView? BoxSelecionado; //pode ser nulo se um novo filme
        public string? _diretorioFilmes;

        private FileInfo? _filmeSelecionado;//Arquivo do filme
        private string? _dirImagemSelecionada;//Imagem selecionada


        public FormularioView()
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
            op.Filter = "Arquivos de imagem|*.jpg;*.jpeg;*.png;*.webp";

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
            CancelarHandler?.Invoke(this, EventArgs.Empty);
        }

        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (SalvarDados())
            {
                SalvarHandler?.Invoke(_filmeSelecionado, EventArgs.Empty);
            }
        }

        private void btnPesquisarCapa_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PesquisaCapaWeb();
        }

        private void btnPesquisarFilme_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PesquisaFilmeSinopseWeb();
        }

        private void btnSelecionarFilme_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Selecione um arquivo de vídeo";
            op.Filter = "Arquivos de vídeo|*.mp4;*.avi;*.mov;*.wmv;*.mkv";

            DialogResult result = op.ShowDialog();

            if (result == DialogResult.OK)
            {
                FileInfo fileInfo = new FileInfo(op.FileName);
                if (fileInfo.Exists)
                {
                    CarregarDados(fileInfo);
                }
            }
        }

        #endregion

        #region Funcoes Formulario

        public void LimparVariaveis()
        {
            BoxSelecionado = null;
            _filmeSelecionado = null;
            _diretorioFilmes = null;
            _dirImagemSelecionada = null;

            txbDirArquivo.Text = "selecione um arquivo";
            txbArquivo.Text = "";
            txbNome.Text = "";
            txbSinopse.Text = "";
            txbTrailer.Text = "";

            CarregarImagemNoGrid("pack://application:,,,/Resources/box/capa.jpg"); //Imagem padrao
        }

        public void CarregarDados(FileInfo arquivo)
        {
            _filmeSelecionado = arquivo;

            string NomeArqFilme = Path.GetFileNameWithoutExtension(_filmeSelecionado.Name);
            string DirArqFilme = _filmeSelecionado.DirectoryName ?? "";

            //Carrega local do arquivo
            txbDirArquivo.Text = _filmeSelecionado.FullName;
            //Carrega nome Arquivo
            txbArquivo.Text = NomeArqFilme;
            //Carrega a imagem
            FileInfo fileImagem = new FileInfo(Path.Combine(DirArqFilme, NomeArqFilme + ".jpg"));
            if (fileImagem.Exists)
            {
                // Criar a URI da imagem com o identificador único
                string uriImagem = "file:///" + fileImagem.FullName.Replace("\\", "/") + "?" + DateTime.Now.Ticks;
                CarregarImagemNoGrid(uriImagem);
            }
            //Carrega os infos
            InfosFilme infosFilme = InfosServicos.ReadFromJsonFile<InfosFilme>(DirArqFilme, NomeArqFilme) ?? new InfosFilme();
            txbNome.Text = infosFilme.Nome;
            txbSinopse.Text = infosFilme.Sinopse;
            txbTrailer.Text = infosFilme.IDVideoYoutube;
        }

        private bool SalvarDados()
        {
            if (string.IsNullOrEmpty(_diretorioFilmes)) return false;

            //---- Valida e exibe os alertas os dados antes de salvar ----//
            if (ValidarDados() == false) return false;

            if (_filmeSelecionado == null ||
                string.IsNullOrEmpty(txbArquivo.Text.Replace(" ", "")) ||
                string.IsNullOrEmpty(txbNome.Text.Replace(" ", "")))
            {
                return false;
            }

            FileInfo fileAtual = _filmeSelecionado;

            string extensao = System.IO.Path.GetExtension(fileAtual.FullName);
            string nome_do_arquivo = txbArquivo.Text;
            string nome_filme = txbNome.Text;

            //Cria uma pasta com nome do arquivo e recorta o filme e joga la
            string diretorio_final = System.IO.Path.Combine(_diretorioFilmes, nome_filme);
            if (!Directory.Exists(diretorio_final))
                Directory.CreateDirectory(diretorio_final);

            //Se não tiver o arquivo na pasta selecionada move para a pasta com novo nome já
            if (!File.Exists(System.IO.Path.Combine(diretorio_final, fileAtual.Name)))
            {
                File.Move(fileAtual.FullName, System.IO.Path.Combine(diretorio_final, nome_do_arquivo + extensao));
                _filmeSelecionado = fileAtual = new FileInfo(System.IO.Path.Combine(diretorio_final, nome_do_arquivo + extensao));
            }

            //Verifica se alterou o nome do arquivo com o arquivo local
            if (nome_do_arquivo != System.IO.Path.GetFileNameWithoutExtension(fileAtual.Name))
            {
                File.Move(fileAtual.FullName, System.IO.Path.Combine(diretorio_final, nome_do_arquivo + extensao));
                 fileAtual = new FileInfo(System.IO.Path.Combine(diretorio_final, nome_do_arquivo + extensao));
            }

            //Salva nova imagem
            if (_dirImagemSelecionada != null)
            {
                File.Copy(_dirImagemSelecionada, System.IO.Path.Combine(diretorio_final, nome_do_arquivo + ".jpg"), true); //sobreescreve se já estiver la
            }

            //Salva as infos
            InfosServicos.SaveToJsonFile(diretorio_final, nome_do_arquivo, new InfosFilme()
            {
                Nome = txbNome.Text,
                IDVideoYoutube = txbTrailer.Text,
                Sinopse = txbSinopse.Text
            });

            //Se teve mudanca apaga os dados anteriores
            if (BoxSelecionado != null && BoxSelecionado.ArquivoFilme != null && BoxSelecionado.ArquivoFilme != fileAtual)
            {
                string nome_Old = System.IO.Path.GetFileNameWithoutExtension(BoxSelecionado.ArquivoFilme.Name);
                string diretorio_Old = BoxSelecionado.ArquivoFilme.DirectoryName ?? "";

                //Verifica a imagem 
                if (File.Exists(System.IO.Path.Combine(diretorio_Old, nome_Old + ".jpg")))
                {
                    if (_dirImagemSelecionada != null) //se carregou nova deleta
                    {
                        File.Delete(System.IO.Path.Combine(diretorio_Old, nome_Old + ".jpg"));
                    }
                    else //se move / renomeia
                    {
                        File.Move(System.IO.Path.Combine(diretorio_Old, nome_Old + ".jpg"), System.IO.Path.Combine(diretorio_final, nome_do_arquivo + ".jpg"), true);
                    }
                }

                //Deleta as infos com nome anterior
                File.Delete(System.IO.Path.Combine(diretorio_Old, nome_Old + ".json"));

                //Deleta o diretorio se nao for o mesmo
                try
                {
                    //pode ter algo dentro da pasta alem do filme entao nao vao conseguir deletar
                    if (diretorio_Old != fileAtual.DirectoryName)
                        Directory.Delete(diretorio_Old);
                }
                catch { }

                BoxSelecionado.CarregarDados(fileAtual);
            }

            return true; //chegou aqui ocorreu tudo bem!
        }

        private bool ValidarDados()
        {
            if (_filmeSelecionado == null)
            {
                ExibirMensagem("Selecione um filme para poder salvar");
                return false;
            }

            if (string.IsNullOrEmpty(txbArquivo.Text.Replace(" ", "")))
            {
                ExibirMensagem("Nome arquivo precisa ser preenchido!");
                return false;
            }

            if (string.IsNullOrEmpty(txbNome.Text.Replace(" ", "")))
            {
                ExibirMensagem("Nome do filme é obrigatório!");
                return false;
            }

            if (!IsValidDirectoryName(txbNome.Text))
            {
                ExibirMensagem("Nome so pode conter apenas letras, números, espaços ou traços");
                return false;
            }

            return true;
        }

        static bool IsValidDirectoryName(string nome)
        {
            // Expressão regular para verificar se a string contém apenas letras, números, espaços ou traços
            Regex regex = new Regex("^[a-zA-Z0-9\\s\\-]*$");

            // Verifique se a string corresponde à expressão regular
            return regex.IsMatch(nome);
        }

        private void ExibirMensagem(string mensagem)
        {
            System.Windows.MessageBox.Show(mensagem);
        }

        #endregion

        #region Funcoes

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
            if (_filmeSelecionado == null) return;

            string valor = string.IsNullOrEmpty(txbNome.Text.Replace(" ", "")) ? System.IO.Path.GetFileNameWithoutExtension(_filmeSelecionado.Name) : txbNome.Text;

            //da prioridade pro nome do filme mas se for nulo pesquisa pelo nome do arquivo
            string termoPesquisa = $"Capa {valor}";

            // Construa a URL de pesquisa do Google Imagens com o termo de pesquisa
            string urlPesquisa = $"https://www.google.com/search?q={HttpUtility.UrlEncode(termoPesquisa)}&tbm=isch";

            // Abra a URL no navegador padrão
            Process.Start(new ProcessStartInfo
            {
                FileName = urlPesquisa,
                UseShellExecute = true
            });
        }

        private void PesquisaFilmeSinopseWeb()
        {
            if (_filmeSelecionado == null) return;

            string valor = System.IO.Path.GetFileNameWithoutExtension(_filmeSelecionado.Name);

            //da prioridade pro nome do filme mas se for nulo pesquisa pelo nome do arquivo
            string termoPesquisa = $"Sinopse Filme {valor}";
            // Construa a URL de pesquisa do Google Imagens com o termo de pesquisa
            string urlPesquisa = $"https://www.google.com/search?q={HttpUtility.UrlEncode(termoPesquisa)}";

            // Abra a URL no navegador padrão
            Process.Start(new ProcessStartInfo
            {
                FileName = urlPesquisa,
                UseShellExecute = true
            });
        }

        #endregion
    }
}
