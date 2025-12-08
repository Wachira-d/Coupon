<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="Coupon.Admin.Report" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <p>
        <asp:TextBox ID="TextBox1" runat="server" Width="30%" placeholder="Email"></asp:TextBox>
    <p>
        <asp:TextBox ID="TextBox13" runat="server" Width="30%" placeholder="Password" TextMode="Password"></asp:TextBox>
    <p>
&nbsp;<asp:Button ID="Button1" runat="server" Text="Login" OnClick="Button1_Click" Width="30%" />
        <asp:Panel ID="Panel1" runat="server" Visible="False">

             <br />
 <table style="width:90%;">
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end"><strong>Counpon Send: </strong> </td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">
             <strong>
             <asp:Label ID="Label3" runat="server" Text="Label"></asp:Label>
             </strong>
         </td>
         <td style="width:15%; height: 20px;"><strong>Counpon Used: </strong></td>
         <td style="width:35%; height: 20px;"><strong>
             <asp:Label ID="Label1" runat="server" Text="Label"></asp:Label>
             </strong></td>
     </tr>
     <tr>
         <td>&nbsp;</td>
         <td>&nbsp;</td>
         <td>&nbsp;</td>
         <td>&nbsp;</td>
     </tr>
     <tr>
         <td style="height: 20px">
             <asp:Button ID="Button3" runat="server" OnClick="Button3_Click" Text="Clear Result" Width="50%" />
         </td>
         <td style="height: 20px">
             </td>
         <td style="height: 20px"></td>
         <td style="height: 20px"></td>
     </tr>
     <tr>
         <td style="height: 20px" colspan="4"><hr /></td>
     </tr>
     <tr>
         <td style="height: 20px">&nbsp;</td>
         <td style="height: 20px">&nbsp;</td>
         <td style="height: 20px">&nbsp;</td>
         <td style="height: 20px">&nbsp;</td>
     </tr>
     <tr>
         <td>ID:</td>
         <td>
             <asp:TextBox ID="TextBox2" runat="server" Width="50%"></asp:TextBox>
         </td>
         <td>Code:</td>
             <td>
                 <asp:TextBox ID="TextBox3" runat="server" Width="50%"></asp:TextBox>
             </td>
     </tr>
     <tr>
         <td>Coupon Reister from:</td>
         <td>
             <asp:TextBox ID="TextBox4" runat="server" Width="50%" TextMode="Date"></asp:TextBox>
         </td>
         <td>Coupon Reister to:</td>
         <td>
             <asp:TextBox ID="TextBox5" runat="server" Width="50%" TextMode="Date"></asp:TextBox>
         </td>
     </tr>
     <tr>
         <td>Coupon Send from:</td>
         <td>
             <asp:TextBox ID="TextBox6" runat="server" Width="50%" TextMode="Date"></asp:TextBox>
         </td>
         <td>Coupon Send to:</td>
         <td>
             <asp:TextBox ID="TextBox7" runat="server" Width="50%" TextMode="Date"></asp:TextBox>
         </td>
     </tr>
     <tr>
         <td>Coupon Used from:</td>
         <td>
             <asp:TextBox ID="TextBox8" runat="server" Width="50%" TextMode="Date"></asp:TextBox>
         </td>
         <td>Coupon Used to:</td>
         <td>
             <asp:TextBox ID="TextBox9" runat="server" Width="50%" TextMode="Date"></asp:TextBox>
         </td>
     </tr>
     <tr>
         <td>Send Status:</td>
         <td>
             <asp:DropDownList ID="DropDownList1" runat="server" Width="50%">
                 <asp:ListItem>All</asp:ListItem>
                 <asp:ListItem>True</asp:ListItem>
                 <asp:ListItem>False</asp:ListItem>
             </asp:DropDownList>
         </td>
         <td>Used Status:</td>
         <td>
             <asp:DropDownList ID="DropDownList2" runat="server" Width="50%">
                 <asp:ListItem>All</asp:ListItem>
                 <asp:ListItem>True</asp:ListItem>
                 <asp:ListItem>False</asp:ListItem>
             </asp:DropDownList>
         </td>
     </tr>
     <tr>
         <td>&nbsp;</td>
         <td>&nbsp;</td>
         <td>
             <asp:Label ID="Label2" runat="server" Text=""></asp:Label>
         </td>
         <td>
             <asp:Button ID="Button2" runat="server" Text="Search Coupon Status" Width="50%" OnClick="Button2_Click" Height="50px" />
         </td>
     </tr>
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">&nbsp;</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">&nbsp;</td>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">&nbsp;</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">&nbsp;</td>
     </tr>
     <tr>
         <td colspan="4" style="height: 20px;text-align:end;align-items:self-end"><hr /></td>
     </tr>
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">&nbsp;</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">&nbsp;</td>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">&nbsp;</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">&nbsp;</td>
     </tr>
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">Log from:</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">
             <asp:TextBox ID="TextBox10" runat="server" TextMode="Date" Width="50%"></asp:TextBox>
         </td>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">Log to:</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">
             <asp:TextBox ID="TextBox11" runat="server" TextMode="Date" Width="50%"></asp:TextBox>
         </td>
     </tr>
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">ID:</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">
             <asp:TextBox ID="TextBox12" runat="server" Width="50%"></asp:TextBox>
         </td>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">Found Coupon</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">
             <asp:CheckBox ID="CheckBox3" runat="server" Checked="true" />
         </td>
     </tr>
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end"></td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start"></td>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">Redeem Coupon</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">
             <asp:CheckBox ID="CheckBox4" runat="server" />
         </td>
     </tr>
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end"></td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start"></td>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end"></td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">
             <asp:Button ID="Button4" runat="server"  Text="Search Coupon Logs" Width="50%" OnClick="Button4_Click" Height="50px" />
         </td>
     </tr>
     <tr>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">&nbsp;</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">&nbsp;</td>
         <td style="width:15%; height: 20px;text-align:end;align-items:self-end">&nbsp;</td>
         <td style="width:35%; height: 20px;text-align:start;align-items:self-start">&nbsp;</td>
     </tr>
 </table>

        </asp:Panel>
       
    </p>

    <asp:GridView ID="GridView1" runat="server"></asp:GridView>
</asp:Content>
