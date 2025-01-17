﻿using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Stx.ThreeSixtyfyer
{
    public class GitHubBasedUpdateCheck
    {
        //https://raw.githubusercontent.com/procedure1/Beat-360fyer/master/Build/latestVersion.txt

        public string repoOwner;
        public string repoName;
        public string branch = "master";
        public string versionFilePath;

        public string VersionFileUrl => $"https://raw.githubusercontent.com/{ repoOwner }/{ repoName }/{ branch }/{ versionFilePath }";

        public GitHubBasedUpdateCheck(string repoOwner, string repoName, string versionFilePath)
        {
            this.repoOwner = repoOwner;
            this.repoName = repoName;
            this.versionFilePath = versionFilePath;
        }

        public async Task<bool> CheckForUpdate(string currentVersion)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string redVersion = await client.GetStringAsync(VersionFileUrl);
                    Console.WriteLine("Current version: " + currentVersion);
                    Console.WriteLine("Read version: " + redVersion);
                    return SemVersion.Parse(redVersion) > currentVersion;
                }
            }
            catch
            {
                return false;
            }

        }
    }
}
