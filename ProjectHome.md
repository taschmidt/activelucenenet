# Overview #

ActiveLucene.Net is a collection of classes to facilitate developing with Lucene.Net. At its core is the ability to easily and automatically convert between a Lucene Document object and a POCO through the use of attributes.

**As of 8/2013, ActiveLucene.Net has been updated to support Lucene 3.x.  Also, all binary releases are now distributed via [NuGet](https://www.nuget.org/packages/ActiveLucene.Net/).**

# Features #

  * Use LuceneFieldAttributes on the properties of any data-bearing class and easily convert to and from a Lucene Document object using the LuceneMediator class.
  * LuceneMediator caches Lucene Field objects avoiding costly object creation which can dramatically speed up population of large indexes.
  * The IndexManager class provides a generic implementation of single file-based staging index, allowing background rebuilding and scoped locking semantics

# Getting Started #

Go grab the NuGet package [here](https://www.nuget.org/packages/ActiveLucene.Net/).

## LuceneMediator ##

First, decorate the properties of any data-bearing class with LuceneField attributes:

```
    public class TestRecord
    {
        [LuceneField("data")]
        public string Data { get; set; }

        [LuceneField("data2")]
        public string Data2 { get; set; }
    }
```

Then, use the LuceneMediator to set up a Lucene Document for insertion into any IndexWriter:

```
    var obj = new TestRecord { Data = "foo", Data2 = "bar" };
    var doc = LuceneMediator<TestRecord>.ToDocument(obj);
```

LuceneMediator can also be used in reverse, for converting a Lucene Document into your record class:

```
    var record = LuceneMediator<TestRecord>.ToRecord(doc);
```

## IndexManager ##

The IndexManager class provides a generic implementation of a single file-based staging index, allowing background rebuilding and scoped locking semantics.

Sample of adding documents:

```
    var indexManager = new IndexManager(path, analyzer);
    indexManager.Open();

    using(var indexWriter = indexManager.GetIndexWriter())
    {
        indexWriter.AddDocument(new Document());
    }

    indexManager.Close();
```

Sample of searching:

```
    using(var indexSearcher = indexManager.GetIndexSearcher())
    {
        var doc = indexSearcher.Search(query, 10);
    }
```