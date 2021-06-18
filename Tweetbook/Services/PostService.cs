using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

using Tweetbook.Data;
using Tweetbook.Domain;

namespace Tweetbook.Services
{
	public class PostService : IPostService
	{
		private readonly DataContext _dataContext;

		public PostService(DataContext dataContext)
		{
			_dataContext = dataContext;
		}

		public Task<List<Post>> GetPostsAsync()
		{
			return Task.FromResult(_dataContext.Posts.ToList());
		}

		public Task<Post> GetPostByIdAsync(Guid postId)
		{
			return Task.FromResult(_dataContext.Posts.SingleOrDefault(post => post.Id == postId));
		}

		public async Task<bool> CreatePostAsync(Post postToCreate)
		{
			await _dataContext.Posts.AddAsync(postToCreate);
			var recordsAffected = await _dataContext.SaveChangesAsync();

			return recordsAffected > 0;
		}

		public async Task<bool> UpdatePostAsync(Post postToUpdate)
		{
			var post = await GetPostByIdAsync(postToUpdate.Id);

			if (post == null)
				return false;

			_dataContext.Posts.Update(postToUpdate);
			var recordsAffected = await _dataContext.SaveChangesAsync();

			return recordsAffected > 0;
		}

		public async Task<bool> DeletePostAsync(Guid postId)
		{
			var post = await GetPostByIdAsync(postId);

			if (post == null)
				return false;

			_dataContext.Posts.Remove(post);
			var recordsAffected = await _dataContext.SaveChangesAsync();

			return recordsAffected > 0;
		}

		public Task<bool> IsUserOwnsPostAsync(Guid postId, string userId)
		{
			var post = _dataContext
				.Posts
				.AsNoTracking()
				.SingleOrDefault(post =>
					post.Id == postId && 
					post.UserId == userId
				);

			return Task.FromResult(post != null);
		}
	}
}