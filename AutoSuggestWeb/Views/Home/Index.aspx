<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Home Page
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>
        <%: ViewData["Message"] %></h2>
    <p>
        Copenhagen Marathon 2010 contestants: 
        <input id="autocomplete" autocomplete="off" />
    </p>

    <script type="text/javascript">
        $(document).ready(function () 
        {
            $("input#autocomplete").autocomplete({
                source: "/Home/GetNames",
			    minLength: 2                
            });
        });
    </script>
</asp:Content>
