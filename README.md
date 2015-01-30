#Muse
[![Build status](https://ci.appveyor.com/api/projects/status/m0m9dx7b1h73eot3?svg=true)](https://ci.appveyor.com/project/MichaelTyson/shiny-myty-website)

A simple website and blog engine built on ASP.NET

##Overview
There are two seperate components to Muse:

1. An MVC server powered by [Nancy](https://github.com/NancyFx/Nancy).
2. A Github repository to hold all of your content.

These two components work in tandem. The content repository has a hook setup with a specially formed URL that when any changes are pushed to the repo it sends a POST message to the server.  This Url has a secret key that only the server knows and responds to.  If the key is wrong the message will fail. When the server does receive a valid message from the github repo, it will then pull the content from the repository and scan through it.  For the scanning process, it first creates an in-memory structure backed by JSON files of the pages and posts.  It then creates two static files. One, for the Atom RSS Feed, called atom.xml...and the other for the sitemap, called...sitemap.xml.

##Getting Started
###Server

```
git clone https://github.com/myty/muse.git
cd muse
```

Add a super-secret-sauce file to the root folder of your project and call it **env.config.json** and whatever you do; do _NOT_ commit this to Github.  It holds your secret Github token you will need to pull from your repo of choice.
```json
{
    "baseUrl": "http://mytydev.com",
    "disqus_shortname": "mytyblog",
    "ga_tracking_code":  "UA-xxxxxx-xx",
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
- **ga_tracking_code**: _(optional)_ Google Analytics tracking code
- **refreshToken**: Can be any secret string you can come up with, but keep it a secret, otherwise anyone could be a jerk and totally make the server do a constant poll on your github account which will assuredly fill up your API request limit.
- **gitHubToken**: Is a personal access token that you can get from this page: https://github.com/settings/applications (again keep this secret)
- **sync**: This is object basically sets up which repo you want to pull from and from which branch and which folders you want to pull down to the server.  The localStoragePath will set the location where the files will be stored on the server before the scanning process. More information on how to set this section up is in the section of the README documentation.

###Content
Fork **myty/muse-starter-content:** https://github.com/myty/muse-starter-content/fork

In your newly forked content repository, Go to Settings -> Webhooks & Services -> Add Webhook
- **Payload URL:** http://{baseUrl}/update?key={refreshToken} <br/>_(replace {baseUrl} and {refreshToken} with what you previously setup in  **env.config.json**)_.
- This is what will trigger the server to pull down fresh documents and whatever else you have set to be synced from your content repo

###Additional Notes
You can test the Payload URL on your local machine using a tool like [Postman](https://chrome.google.com/webstore/detail/postman-rest-client-packa/fhbjgbiflinjbdggehcddcbncdddomop).  Instead of using the baseUrl from env.config.json, you'd just put in the local address. _For example: http://localhost:5001/update?key=supersecretkey_


##Motivation For Muse
1. Partly because it was needed and partly to see if it could be done.
2. What better way to get involved in the developer community than releasing some code to the public.

##Sites Using Muse:
- http://mytydev.com
- _Your Site Goes Here_
