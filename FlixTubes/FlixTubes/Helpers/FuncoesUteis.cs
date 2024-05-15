using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;

namespace FlixTubes.Helpers
{
    public static class FuncoesUteis
    {
        public static void CarregarImagemNoGrid(Grid grid, string dirImagem, Stretch stretch)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; //livra o cache para caso va fazer transformacoes na imagem
            bitmap.UriSource = new Uri(dirImagem);
            bitmap.EndInit();
            grid.Background = new ImageBrush(bitmap) { Stretch = stretch };
        }
    }
}
