using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Baseline;
using StorytellerDocGen.Runner;
using StoryTeller.Files;

namespace StorytellerDocGen.Samples
{
    public class SampleBuilder : ISampleBuilder
    {
        private readonly IBrowserRefresher _browser;
        private readonly ISampleCache _cache;

        private readonly LightweightCache<string, ISampleScanner> _scanners = new LightweightCache<string, ISampleScanner>();
        private readonly IList<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();


        public SampleBuilder(ISampleCache cache, IBrowserRefresher browser, IEnumerable<ISampleScanner> scanners)
        {
            _cache = cache;
            _browser = browser;

            scanners.Each(x =>
            {
                var ext = x.Extension.ToLower();
                if (!ext.StartsWith(".")) ext = "." + ext;

                _scanners[ext] = x;
            });
        }

        public Task ScanFolder(string folder)
        {
            var tasks = _scanners.SelectMany(scanner =>
            {
                var filter = "*" + scanner.Extension;
                var watcher = new FileSystemWatcher(folder, filter) {NotifyFilter = NotifyFilters.LastWrite};
                watcher.Created += FileChanged;
                watcher.Changed += FileChanged;

                watcher.EnableRaisingEvents = true;
                watcher.IncludeSubdirectories = true;

                _watchers.Add(watcher);

                var files = FindFiles(folder, filter);
                return files.Select(x => { return Task.Factory.StartNew(() => { ReadFile(scanner, x); }); });
            });

            return
                Task.WhenAll(tasks)
                    .ContinueWith(t => { Console.WriteLine("Sample watching ready for directory " + folder); });
        }

        public void Dispose()
        {
            _watchers.Each(x => x.Dispose());
        }

        public static IEnumerable<string> FindFiles(string directory, string ext)
        {
            return new FileSystem().FindFiles(directory, FileSet.Deep("*." + ext));
        }

        private Task readFiles(IEnumerable<string> files)
        {
            var tasks = files.Select(x =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var ext = Path.GetExtension(x).ToLower();
                    var scanner = _scanners[ext];
                    ReadFile(scanner, x);
                });
            });

            return Task.WhenAll(tasks);
        }

        private void FileChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            var file = fileSystemEventArgs.FullPath;
            readFiles(new[] {file}).ContinueWith(t => _browser.RefreshPage());
        }


        public bool ReadFile(ISampleScanner scanner, string file)
        {
            var reader = new SampleReader(new FileReference(file), scanner, _cache);
            reader.Start();

            return reader.FoundSamples;
        }


        public class ChangeHandler : IChangeSetHandler
        {
            private readonly SampleBuilder _parent;
            private readonly string _root;

            public ChangeHandler(string root, SampleBuilder parent)
            {
                _root = root;
                _parent = parent;
            }

            public void Handle(ChangeSet changes)
            {
                var added = _parent.readFiles(changes.Added.Select(x => _root.AppendPath(x.RelativePath)));
                var modified = _parent.readFiles(changes.Changed.Select(x => _root.AppendPath(x.RelativePath)));

                Task.WhenAll(added, modified).ContinueWith(t => _parent._browser.RefreshPage());
            }
        }
    }
}