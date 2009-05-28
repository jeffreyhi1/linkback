<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="indexTitle" ContentPlaceHolderID="TitleContent" runat="server">
    Home Page
</asp:Content>

<asp:Content ID="indexContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%= Html.Encode(ViewData["Message"]) %></h2>
    <p>
        This application shows the use of <a href="http://code.google.com/p/linkback/">Linkback.NET library</a> for sending/receiving <a href="http://en.wikipedia.org/wiki/Trackback">trackbacks</a> and <a href="http://en.wikipedia.org/wiki/Pingback">pingbacks</a>.
    </p>
    <p>
        <a href="http://en.wikipedia.org/wiki/Markdown">Markdown</a> is used for posts editing. See the <a href="http://daringfireball.net/projects/markdown/syntax">full Markdown syntax</a> for more information.
    </p>
    <p>
        To learn more about Linkback.NET visit <a href="http://code.google.com/p/linkback/" title="Linkback.NET Website">http://code.google.com/p/linkback/</a>.
    </p>
</asp:Content>