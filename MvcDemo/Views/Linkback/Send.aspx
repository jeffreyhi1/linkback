<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<IEnumerable<string>>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
	Send
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Send <%= ViewData["Linkback-Name"] %></h2>
    <p><%= TempData["Linkback-Send-Result"] %></p>
    <% using (Html.BeginForm()) { %>
    <fieldset>
        <ul style="list-style-type:none;">
            <% foreach (string url in Model) { %>
            <li>
                <%= Html.RadioButton("url", url)%><%= url%>
            </li>
            <% } %>
            <% if (ViewData["Linkback-Name"].ToString() == "Trackback") { %>
            <li>
                <%= Html.CheckBox("autodiscovery", true) %> autodiscovery
            </li>
            <% } %>
            <li>
                <label for="submit">&nbsp;</label>
                <input id="submit" name="submit" type="submit" value="Send" />
            </li>
        </ul>
    </fieldset>
    <% } %>
</asp:Content>