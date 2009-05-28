<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<MvcDemo.Models.Post>" %>

<div class="post">
    <h3><%= Html.ActionLink(Model.Title, "Details", new { Model.Id }) %></h3>
    <h4><%= String.Format("{0:g}", Model.Created) %></h4>
    <div><%= new anrControls.Markdown().Transform(Model.Content) %></div>
    <p>
        <%= Html.RouteLink("Send trackback", "Trackback-Send", new { Model.Id }) %> |
        <%= Html.RouteLink("Send pingback", "Pingback-Send", new { Model.Id }) %> |
        <%= Html.ActionLink("Edit", "Edit", new { Model.Id }) %> |
        <%= Html.ActionLink("Delete", "Delete", new { Model.Id }) %> |
        <strong><%= Model.Comments.Count %></strong>&nbsp;comments;&nbsp;Use&nbsp;<%= Html.RouteLink("this", "Trackback-Receive", new { Model.Id })%>&nbsp;url&nbsp;for&nbsp;trackbacks
    </p>
</div>