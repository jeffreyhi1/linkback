<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<MvcDemo.Models.Post>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
	Delete
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Delete post ?</h2>
    <% using (Html.BeginForm()) {%>
        <fieldset>
        <legend>Fields</legend>
        <p>
            Id:
            <%= Html.Encode(Model.Id) %>
        </p>
        <p>
            Created:
            <%= Html.Encode(String.Format("{0:g}", Model.Created)) %>
        </p>
        <p>
            Content:
            <%= new anrControls.Markdown().Transform(Model.Content) %>
        </p>
        <p>
            <input type="submit" value="Delete" />
        </p>
    </fieldset>
    <% } %>
    <p>
        <%=Html.ActionLink("Back to List", "Index") %>
    </p>
</asp:Content>

