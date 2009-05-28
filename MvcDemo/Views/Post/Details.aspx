<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<MvcDemo.Models.Post>" %>
<%@ Import Namespace="LinkbackNet" %>
<%@ Import Namespace="MvcDemo.Controllers" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
	Details
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="details">
        <h2>Details</h2>
        <%= "<!--" + Trackback.DeclareServiceInHtml(Request.Url, "Title", new Uri(Url.AbsoluteRouteUrl("Trackback-Receive", new { Model.Id }))) + "-->" %>
        <% Html.RenderPartial("Post", Model); %>
        <ul class="comments">
            <% foreach (var comment in Model.Comments.OrderByDescending(x => x.Created)) { %>
            <li class="comment">
                <h4><%= Html.Encode(comment.From) %></h4>
                <h3><%= String.Format("{0:g}", comment.Created) %></h3>
                <div><%= comment.Content %></div>
                <p><%= Html.ActionLink("Delete", "Delete", new { controller = "Comment", id = comment.Id }) %></p>
            </li>
            <% } %>
        </ul>
        <p>
            <%=Html.ActionLink("Back to List", "Index") %>
        </p>
    </div>
</asp:Content>