using System.Security.Claims;
using Core.Dtos;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace learnathon_learning_phase.Services
{
    public class TweetService : ITweetService
    {

        private readonly IMongoCollection<Tweets> _tweetCollection;
        private readonly IMongoCollection<HashTags> _hashTagCollection;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TweetService(IOptions<TwitterCloneDbConfig> twitterCloneDbConfig, IMongoClient mongoClient, IHttpContextAccessor httpContextAccessor)
        {
            var mongoDatabase = mongoClient.GetDatabase(twitterCloneDbConfig.Value.DatabaseName);

            _tweetCollection = mongoDatabase.GetCollection<Tweets>(twitterCloneDbConfig.Value.TweetCollectionName);
            _hashTagCollection = mongoDatabase.GetCollection<HashTags>(twitterCloneDbConfig.Value.HashTagCollectionName);
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<Tweets?> CreateTweet(TweetRequestDto tweet)
        {
            Tweets? tweetModel = null;
            if (_httpContextAccessor.HttpContext != null)
            {
                string? userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    tweetModel = new Tweets
                    {
                        UserId = userId,
                        Tweet = tweet.Tweet,
                    };
                    await _tweetCollection.InsertOneAsync(tweetModel);

                    foreach (var hashTag in tweet.HashTags)
                    {
                        var hashTagModel = new HashTags

                        {
                            HashTag = hashTag,
                            TweetId = tweetModel.Id,
                        };
                        await _hashTagCollection.InsertOneAsync(hashTagModel);
                    }

                }
            }
            return tweetModel;
        }

        public async Task<Tweets?> GetTweetById(string id)
        {
            Tweets? tweet = null;
            if (_httpContextAccessor.HttpContext != null)
            {
                string? userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    tweet = await _tweetCollection.Find(tweet => tweet.Id == id && tweet.DeletedAt == null).FirstOrDefaultAsync();
                }
            }
            return tweet;

        }

        public async Task<Tweets> UpdateTweet(Tweets tweet, TweetRequestDto tweetRequest)
        {
            if (tweet.History.Length == 0)
            {
                tweet.History = new string[1];
                tweet.History[0] = tweet.Tweet;
            }
            else
            {
                tweet.History = tweet.History.Append(tweet.Tweet).ToArray();
            }
            tweet.Tweet = tweetRequest.Tweet;
            tweet.UpdatedAt = DateTime.Now;
            await _tweetCollection.ReplaceOneAsync(t => t.Id == tweet.Id, tweet);
            await _hashTagCollection.DeleteManyAsync(hashTag => hashTag.TweetId == tweet.Id);
            foreach (var hashTag in tweetRequest.HashTags)
            {
                var hashTagModel = new HashTags
                {
                    HashTag = hashTag,
                    TweetId = tweet.Id,
                };
                await _hashTagCollection.InsertOneAsync(hashTagModel);
            }
            return tweet;
        }

        public async Task DeleteTweet(Tweets tweet)
        {
            tweet.DeletedAt = DateTime.Now;
            await _tweetCollection.ReplaceOneAsync(t => t.Id == tweet.Id, tweet);
            await _hashTagCollection.DeleteManyAsync(hashTag => hashTag.TweetId == tweet.Id);
        }
    }


}