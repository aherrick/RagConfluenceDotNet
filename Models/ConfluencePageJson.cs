namespace RagConfluenceDotNet.Models;

public class ConfluencePageJson
{
    public class Rootobject
    {
        public Result[] results { get; set; }
        public int start { get; set; }
        public int limit { get; set; }
        public int size { get; set; }
        public _Links _links { get; set; }
    }

    public class _Links
    {
        public string _base { get; set; }
        public string context { get; set; }
        public string self { get; set; }
        public string next { get; set; }
    }

    public class Result
    {
        public string id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public string title { get; set; }
        public Macrorenderedoutput macroRenderedOutput { get; set; }
        public Body body { get; set; }
        public _Expandable2 _expandable { get; set; }
        public _Links1 _links { get; set; }
    }

    public class Macrorenderedoutput
    { }

    public class Body
    {
        public Storage storage { get; set; }
        public _Expandable1 _expandable { get; set; }
    }

    public class Storage
    {
        public string value { get; set; }
        public string representation { get; set; }
        public object[] embeddedContent { get; set; }
        public _Expandable _expandable { get; set; }
    }

    public class _Expandable
    {
        public string content { get; set; }
    }

    public class _Expandable1
    {
        public string editor { get; set; }
        public string atlas_doc_format { get; set; }
        public string view { get; set; }
        public string export_view { get; set; }
        public string styled_view { get; set; }
        public string dynamic { get; set; }
        public string editor2 { get; set; }
        public string anonymous_export_view { get; set; }
    }

    public class _Expandable2
    {
        public string container { get; set; }
        public string metadata { get; set; }
        public string restrictions { get; set; }
        public string history { get; set; }
        public string version { get; set; }
        public string descendants { get; set; }
        public string space { get; set; }
        public string childTypes { get; set; }
        public string schedulePublishInfo { get; set; }
        public string operations { get; set; }
        public string schedulePublishDate { get; set; }
        public string children { get; set; }
        public string ancestors { get; set; }
    }

    public class _Links1
    {
        public string self { get; set; }
        public string tinyui { get; set; }
        public string editui { get; set; }
        public string webui { get; set; }
    }
}