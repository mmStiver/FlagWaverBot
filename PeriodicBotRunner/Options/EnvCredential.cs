using DiscuitSharp.Core.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeriodicBotRunner.Options
{
    internal class EnvCredential(){
            public string UserName;
            public String Password;
        
        public static implicit operator Credentials(EnvCredential credential)
            => new Credentials(credential.UserName, credential.Password);
        };
}
