<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<IEnumerable<MvcDemo.Models.Post>>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
	Index
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Index</h2>
    <p>
        <%= Html.ActionLink("Create New", "Create") %>
    </p>
    <ul>
    <% foreach (var item in Model) { %>
        <li><% Html.RenderPartial("Post", item); %></li>
    <% } %>
    </ul>
    <p>
        <%= Html.ActionLink("Create New", "Create") %>
    </p>
</asp:Content>