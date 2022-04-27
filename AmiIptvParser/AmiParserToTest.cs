using AmiIptvParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiIptvParser;

public class AmiParserToTest:AmiParser
{
    public static void CheckCacheTest(StreamReader readerCache)
    {
        Instance.LoadCache(readerCache);
    }
}
