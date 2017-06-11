using System;
using System.Collections.Generic;
using wallabag.Data.Models;

namespace wallabag.Data.Services.MigrationService
{
    public class Migration
    {
        public Action Action { get; set; }
        public Version Version { get; set; }
    }
}