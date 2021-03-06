﻿// MIT License
// 
// Copyright (c) 2017 Alan McGovern
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Octokit;

namespace libgitface.ActionProviders
{
	public class ReviewPullRequestActionProvider : ActionProvider
	{
		class PullRequestComparer : IEqualityComparer<PullRequest>
		{
			public bool Equals (PullRequest x, PullRequest y) => x?.Id == y?.Id;
			public int GetHashCode (PullRequest obj)          => obj?.Id.GetHashCode () ?? 0;
		}

		IObservable<PullRequest> Created {
			get;
		}

		GitHubClient Client {
			get;
		}

		Dictionary<PullRequest, IAction> PullRequests {
			get;
		}

		libgitface.Repository Repository {
			get;
		}

		string[] Usernames {
			get;
		}

		public ReviewPullRequestActionProvider (GitHubClient client, Repository repo, CancellationToken token)
			: this (client, repo, token, Enumerable.Empty<string> ())
		{

		}

		public ReviewPullRequestActionProvider (GitHubClient client, Repository repo, CancellationToken token, IEnumerable<string> usernames)
		{
			if (client == null)
				throw new ArgumentNullException (nameof (client));
			if (repo == null)
				throw new ArgumentNullException (nameof (repo));
			if (usernames == null)
				throw new ArgumentNullException (nameof (usernames));

			Client = client;
			PullRequests = new Dictionary<PullRequest, IAction> (new PullRequestComparer ());
			Repository = repo;
			Usernames = usernames.ToArray ();
		}

		public async override void Refresh ()
		{
			try {
				var prs = await Client.PullRequest.GetAllForRepository (Repository.Owner, Repository.Name);
				foreach (var pr in prs)
					HandlePullRequest (pr);
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}

		}
		void HandlePullRequest (PullRequest pr)
		{
			if (Usernames.Length > 0 && !Usernames.Contains (pr.User.Login))
				return;

			if (pr.State == ItemState.Open) {
				if (PullRequests.ContainsKey (pr))
					return;

				PullRequests.Add (pr, new OpenUrlAction {
					Url = pr.HtmlUrl.OriginalString,
					ShortDescription = $"Review {pr.Title}",
					Tooltip = $"Review pull request in {Repository.Owner}/{Repository.Name}, created by {pr.User.Login}"
				});
			} else {
				if (!PullRequests.Remove (pr))
					return;
			}

			Actions = PullRequests.Values.ToArray ();
		}
	}
}
