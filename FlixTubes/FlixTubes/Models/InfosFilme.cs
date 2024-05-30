using Newtonsoft.Json;
using System.IO;

namespace FlixTubes.Models
{
    public class InfosFilme
    {
        public string? Nome { get; set; }
        public string? Sinopse { get; set; }
        public string? IDVideoYoutube { get; set; }
    }

    public class InfosSerie
    {
        public string? Nome { get; set; }
        public string? Sinopse { get; set; }
    }

    public static class InfosServicos
    {
        public static void SaveToJsonFile(string directory,string nomeArquivo, object data)
        {
            string filePath = Path.Combine(directory, $"{nomeArquivo}.json");

            // Serializa o objeto para formato JSON
            string json = JsonConvert.SerializeObject(data);

            // Escreve ou sobrescreve o arquivo
            File.WriteAllText(filePath, json);
        }

        public static T? ReadFromJsonFile<T>(string directory, string nomeArquivo)
        {
            string filePath = Path.Combine(directory, $"{nomeArquivo}.json");

            // Verifica se o arquivo existe
            if (!File.Exists(filePath))
                return default;

            // Lê todo o conteúdo do arquivo
            string json = File.ReadAllText(filePath);

            // Deserializa o conteúdo do arquivo JSON para o tipo especificado
            T? data = JsonConvert.DeserializeObject<T>(json);

            return data;
        }
    }
}
