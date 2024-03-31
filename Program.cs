using System;
using System.IO;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Net.Http;
using System.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WoTArchive
{
    public class Program
    {
        // List of links
        public static List<string> links = new List<string>();

        // List of scraped forum posts
        public static List<Post> scrapedPosts = new List<Post>();

        public static void Main(string[] args)
        {
            // Load all links and sort tem
            links.AddRange(File.ReadAllLines("links.txt"));
            links.Sort();

            // Remove duplicate links (if any)
            List<string> sorted = links.Distinct().ToList();

            // Iterate through each link
            foreach (string link in sorted)
            {
                try
                {
                    // Scrape each link (forum thread)
                    var html = link;
                    HtmlWeb web = new HtmlWeb();
                    var htmlDoc = web.Load(html);

                    // Get the thread title
                    var title = htmlDoc.DocumentNode.SelectSingleNode("//p[@class='largefont']//a").InnerText;

                    // Get all posts in the thread
                    scrapedPosts.Clear();
                    var posts = htmlDoc.DocumentNode.SelectNodes("//div[@class='post']");
                    
                    // Iterate through each forum post in the thread
                    foreach (HtmlNode post in posts)
                    {
                        // Get author
                        string author = post.SelectSingleNode(".//div[@class='username']").InnerText;
                        
                        // Date formatting
                        string date = post.SelectSingleNode(".//div[@class='date']").InnerText;
                        string day = date.Split(" ")[0];
                        if (day.Length == 4) date = date.Remove(2, 2);
                        else if (day.Length == 3) date = date.Remove(1, 2);
                        string newDay = date.Split(" ")[0];
                        if (newDay.Length == 1) date = "0" + date;
                        DateTime dt = DateTime.ParseExact(date, "dd MMMM yyyy, HH:mm", System.Globalization.CultureInfo.InvariantCulture);

                        // Get content
                        string content = post.SelectSingleNode(".//div[@class='posttext']").InnerHtml;

                        // Create new Post
                        Post p = new Post();
                        p.link = link;
                        p.threadTitle = title.ToString();
                        p.datePosted = dt;
                        p.author = author;
                        p.content = content;

                        // Save Post
                        scrapedPosts.Add(p);
                    }

                    // JSON serialize the list of scraped forum posts
                    string json = JsonSerializer.Serialize(scrapedPosts);
                    string pattern = @"t-(\d+)\.html";
                    Match match = Regex.Match(json, pattern);
                    if (match.Success)
                    {
                        // Save each thread to "/threads/<threadId>.json"
                        string name = match.Groups[1].Value;
                        File.WriteAllText(string.Concat(Environment.CurrentDirectory, @"\threads\" + name + ".json"), json);
                        Console.WriteLine($"Saved thread #{name} -> {scrapedPosts.Count} posts found.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        // Forum post class
        public class Post
        {
            public string? link { get; set; }
            public string? threadTitle { get; set; }
            public DateTime datePosted { get; set; }
            public string? author { get; set; }
            public string? content { get; set; }
        }
    }
}