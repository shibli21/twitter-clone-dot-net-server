using System.Security.Claims;
using Core.Dtos;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.Services
{
    public class TweetService : ITweetService
    {

        private readonly IMongoCollection<Tweets> _tweetCollection;
        private readonly IMongoCollection<HashTags> _hashTagCollection;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TweetService(IOptions<TwitterCloneDbConfig> twitterCloneDbConfig, IMongoClient mongoClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            var database = mongoClient.GetDatabase(twitterCloneDbConfig.Value.DatabaseName);
            _tweetCollection = database.GetCollection<Tweets>(twitterCloneDbConfig.Value.TweetCollectionName);
            _hashTagCollection = database.GetCollection<HashTags>(twitterCloneDbConfig.Value.HashTagCollectionName);
        }
        public async Task<Tweets?> CreateTweet(string userId, TweetRequestDto tweet)
        {
            Tweets tweetModel = new Tweets
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
            return tweetModel;
        }

        public async Task<Tweets?> GetTweetById(string id)
        {
            return  await _tweetCollection.Find(tweet => tweet.Id == id && tweet.DeletedAt == null).FirstOrDefaultAsync();
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
        public async Task<Tweets> UpdateRetweet(Tweets tweet, RetweetRequestDto tweetRequest)
        {
            if (tweetRequest.Tweet != null)
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
            }
            else
            {
                tweet.Tweet = "";
            }
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


        public async Task<Tweets?> CreateRetweet(string userId, Tweets originalTweet, RetweetRequestDto tweet)
        {
            Tweets tweetModel = new Tweets
            {
                UserId = userId,
                Tweet = tweet.Tweet,
                Type = "Retweet",
                RetweetRefId = originalTweet.Id,
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

            return tweetModel;

        }

        public async Task<Tweets> UpdateTweetAsync(string id, Tweets tweet)
        {
            await _tweetCollection.ReplaceOneAsync(t => t.Id == id, tweet);
            return tweet;
        }
    }
}