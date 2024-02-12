﻿using System;
using System.Collections.Generic;
using System.Threading;
using Npgsql;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

class Program
{
    static void Main(string[] args)
    {
        // Configuração do WebDriver do Chrome
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--start-maximized");
        options.AddExcludedArgument("disable-popup-blocking");
        IWebDriver driver = new ChromeDriver(options);

        try
        {
            // Acessando a página
            driver.Navigate().GoToUrl("https://10fastfingers.com/typing-test/portuguese");

            // Criando lista de palavras
            int limiteMaximo = 1000;
            List<string> textos = new List<string>();
            for (int i = 1; i <= limiteMaximo; i++)
            {
                string xpath = $"//*[@id='row1']/span[{i}]";
                IWebElement element = null;
                try
                {
                    element = driver.FindElement(By.XPath(xpath));
                }
                catch (NoSuchElementException)
                {
                    break;
                }
                string texto = element.GetAttribute("textContent");
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    textos.Add(texto);
                }
            }

            // Adicionando palavras no campo e separando por espaço
            IWebElement inputField = driver.FindElement(By.XPath("//*[@id='inputfield']"));
            foreach (string texto in textos)
            {
                inputField.SendKeys(texto);
                Thread.Sleep(1);
                inputField.SendKeys(" ");
            }

            // Aguardando 30 segundos
            Thread.Sleep(30000);

            // Criando variáveis para adicionar ao banco de dados
            DateTime dataAtual = DateTime.Now;
            DateTime data = dataAtual.Date;
            string hora = dataAtual.ToString("HH:mm:ss");

            IWebElement ppmElement = driver.FindElement(By.XPath("//*[@id=\"wpm\"]/strong"));
            string ppm = ppmElement.Text;

            IWebElement palavraCertaElement = driver.FindElement(By.XPath("//*[@id=\"correct\"]/td[2]/strong"));
            int palavraCerta = int.Parse(palavraCertaElement.Text);

            IWebElement palavraErradaElement = driver.FindElement(By.XPath("//*[@id=\"wrong\"]/td[2]/strong"));
            int palavraErrada = int.Parse(palavraErradaElement.Text);

            IWebElement percentualElement = driver.FindElement(By.XPath("//*[@id=\"accuracy\"]/td[2]/strong"));
            string percentual = percentualElement.Text;

            IWebElement tecladaCertaElement = driver.FindElement(By.XPath("//*[@id=\"keystrokes\"]/td[2]/small/span[1]"));
            int tecladaCerta = int.Parse(tecladaCertaElement.Text);

            IWebElement tecladaErradaElement = driver.FindElement(By.XPath("//*[@id=\"keystrokes\"]/td[2]/small/span[2]"));
            int tecladaErrada = int.Parse(tecladaErradaElement.Text);

            // Adicionando ao banco de dados
            string connectionString = "Host=localhost;Username=postgres;Password=@Arroba01;Database=postgres";
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO bdados_fastfingers (data, hora, ppm, tecladas_acerto, tecladas_erro, precisao, palavras_corretas, palavras_erradas) VALUES (@data, @hora, @ppm, @tecladas_acerto, @tecladas_erro, @precisao, @palavras_corretas, @palavras_erradas)", conn))
                {
                    cmd.Parameters.AddWithValue("data", data);
                    cmd.Parameters.AddWithValue("hora", hora);
                    cmd.Parameters.AddWithValue("ppm", ppm);
                    cmd.Parameters.AddWithValue("tecladas_acerto", tecladaCerta);
                    cmd.Parameters.AddWithValue("tecladas_erro", tecladaErrada);
                    cmd.Parameters.AddWithValue("precisao", percentual);
                    cmd.Parameters.AddWithValue("palavras_corretas", palavraCerta);
                    cmd.Parameters.AddWithValue("palavras_erradas", palavraErrada);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro durante o teste: " + ex.Message);
        }
        finally
        {
            // Fechando o navegador
            driver.Quit();
        }
    }
}
