<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<MvcDemo.Models.Comment>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
	Delete
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Delete comment ?</h2>
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
            From:
            <%= Html.Encode(Model.From) %>
        </p>
        <p>
            Content:
            <%= Html.Encode(Model.Content) %>
        </p>
        <p>
            <input type="submit" value="Delete" />
        </p>
    </fieldset>
    <% } %>
    <p>
        <%=Html.ActionLink("Back to List", "Index", "Post") %>
    </p>
</asp:Content>

