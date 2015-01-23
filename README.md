#Muse 
[![Build status](https://ci.appveyor.com/api/projects/status/m0m9dx7b1h73eot3?svg=true)](https://ci.appveyor.com/project/MichaelTyson/shiny-myty-website)

A simple website and blog engine built on ASP.NET

##Overview
There are two seperate components to Muse:

1. An MVC server powered by [Nancy](https://github.com/NancyFx/Nancy).
2. A Github repository to hold all of your content. 

These two components work in tandem. The content repository has a hook setup with a specially formed URL that when any changes are pushed to the repo it sends a POST message to the server.  This Url has a secret key that only the server knows and responds to.  If the key is wrong the message will fail. When the server does receive a valid message from the github repo, it will then pull the content from the repository and scan through it.  For the scanning process, it first creates an in-memory structure backed by JSON files of the pages and posts.  It then creates two static files. One, for the Atom RSS Feed, called atom.xml...and the other for the sitemap, called...sitemap.xml.

##Getting Started
git clone https://github.com/myty/muse.git

Add a super-secret-sauce file to the root folder of your project and call it **env.config.json** and whatever you do; do _NOT_ commit this to Github.  It holds your secret Github token you will need to pull from your repo of choice.
```json
{
    "baseUrl": "http://mytydev.com",
    "disqus_shortname": "mytyblog", 
    "refreshToken": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "gitHubToken": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "sync": {
        "owner": "myty",
        "repo": "easily-amused-content",
        "branch": "master",
        "remoteFolders": ["_pages", "_posts", "_imgs", "_site"],
        "locaStoragePath": "~/App_Data/Content"
    }
}
```
- **baseUrl**: Set this to what your site will be
- **disqus_shortname**: To add Disqus support to your site, add your disqus_shortname that's linked to your Disqus account
- **refreshToken**: Can be any secret string you can come up with, but keep it a secret, otherwise anyone could be a jerk and totally make the server do a constant poll on your github account which will assuredly fill up your API request limit.
- **gitHubToken**: Is a personal access token that you can get from this page: https://github.com/settings/applications (again keep this secret)
- **sync**: This is object basically sets up which repo you want to pull from and from which branch and which folders you want to pull down to the server.  The localStoragePath will set the location where the files will be stored on the server before the scanning process.

##Motivation For Muse
1. Partly because it was needed and partly to see if it could be done.
2. What better way to get involved in the developer community than releasing some code to the public.

##Sites Using Muse:
- http://mytydev.com
- _Your Site_
