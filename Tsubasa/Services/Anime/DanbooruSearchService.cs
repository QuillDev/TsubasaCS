using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tsubasa.Helper;

namespace Tsubasa.Services.AnimeServices
{
    public class DanbooruSearchService
    {
        
        private readonly WebRequestService _web;
        private readonly Random _random;
        private const string BaseUrl = "https://danbooru.donmai.us";
        private const int MaxTags = 20;

        public DanbooruSearchService(WebRequestService web)
        {
            _web = web;
            _random = new Random();
        }
        
        /// <summary>
        /// Get random hentai from the given query
        /// </summary>
        /// <param name="query">the query to check for hentai from</param>
        /// <returns>the URL of a hentai image for the given query</returns>
        public async Task<string> GetRandomHentaiAsync(string query)
        {
            var tag = await GetMostPopularTagAsync(query).ConfigureAwait(false);
            return await GetRandomHentaiWithTagAsync(tag).ConfigureAwait(false);
        }
        /// <summary>
        /// Get random hentai from the given tag
        /// </summary>
        /// <param name="tag">The tag to check hentai for</param>
        /// <returns>a hentai url for the current tag</returns>
        public async Task<string> GetRandomHentaiWithTagAsync(string tag)
        {
            //populate requests array
            var response = await _web.RequestJsonAsync($"{BaseUrl}/posts.json?tags= -rating:safe -rating:questionable {tag} &random=true");
            
            //Populate the url list where "large_file_url" is not null or empty
            var urls = response.Select(post => post["large_file_url"]?.ToString()).Where(url => !string.IsNullOrEmpty(url)).ToList();

            //if the urls list length is 0, return null
            return urls.Count == 0 ? null : urls[_random.Next(0, urls.Count)];
        }
        
        /// <summary>
        /// Method that gets the most popular tag for the given query.
        /// </summary>
        /// <param name="query">the query to check tags for</param>
        /// <returns>the most popular tag for the given query</returns>
        public async Task<string> GetMostPopularTagAsync(string query)
        {
            //create list of tags as JTokens
            var tags = await GetTagsAsync(query).ConfigureAwait(false);
            
            //if the tags have a count of zero return null
            if (tags.Count == 0)
            {
                return null;
            }
            
            //set the most popular tag to be the first as a starting point
            var highestPosts = 0;
            var mostPopularTag = "";
            
            //LINQ statement, iterates through all objects where the post count and name are NOT null
            foreach (var tag in tags.Where(tag => tag["name"] != null && tag["post_count"] != null)
                .Where(tag => (int) tag["post_count"] > highestPosts))
            {
                //set new highest values if there aren't any issues
                highestPosts = (int) tag["post_count"];
                mostPopularTag = tag["name"]?.ToString();
            }

            return mostPopularTag;
        }
        
        /// <summary>
        /// Gets all matching tags for the given request
        /// </summary>
        /// <param name="query">the query to check tags for </param>
        /// <returns>all tags matching the given query that respect the max tag length specified</returns>
        public async Task<List<JToken>> GetTagsAsync(string query)
        {
            //Get tags from the query by searching
            var tagsJson = await _web.RequestJsonAsync($"{BaseUrl}/tags.json?search[name_matches]={query}*");
            
            //set the amount of tags to check
            var tagsToCheck = tagsJson.Count;
            
            //if there's more than maxTags tags then set the tags we will search through to maxTags
            if (tagsJson.Count > MaxTags)
            {
                tagsToCheck = MaxTags;
            }
            
            //create list for tags
            var tags = new List<JToken>();
            
            //iterate through all the tags we're checking
            for (var index = 0; index < tagsToCheck; index++)
            {
                tags.Add(tagsJson[index]);
            }

            return tags;
        }
    }
}