<%@ Page Title="Starbucks e-voucher" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Starbucks.aspx.cs" Inherits="Coupon.Starbucks" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
     <style type="text/css">
@font-face {
    font-family: "The Example Font";
    src: url(/fonts/Lotus font/LotussSmartTX-Regular.ttf) format("truetype");
}
div.ExampleFont { 
    font-family: "The Example Font", Lotuss SmartTX-Regular;
}


.button {
  background-color: #006e41; /* Green */
  border: none;
  color: white;
  padding: 15px 32px;
  text-align: center;
  text-decoration: none;
  display: inline-block;
  font-size: 16px;
    -moz-border-radius: 10px;
    -webkit-border-radius: 10px;
    -khtml-border-radius: 10px;
    border-radius: 10px;
}
</style>
    <div class="ExampleFont">
    <asp:Panel ID="Panel1" runat="server"><table style="width:90%;"><tr><td> <table style="width:100%;"><tr><td style="align-items:start;align-content:start;"><img src="./Images/Starbuck/Logo.png" alt="" width="40%" class="navbar-header" /></td><td>&nbsp;</td><td>&nbsp;</td></tr>
        <tr>
            <td style="align-items:start;align-content:start;">
                <asp:Image ID="Image2" runat="server" Width="100%" />
            </td>
            <td>&nbsp;</td>
            <td>&nbsp;</td>
        </tr>
        <center>
        </table></td></tr><tr><td style="align-items:center;align-content:center; class="text-center"><center>
            &nbsp;</center></td></tr><tr><td class="text-center">
            <br />
            <br /> </td></tr><tr><td>
        <p style="font-size:11pt;margin:0;" class="text-left">
            <span lang="th" style="font-size:10pt;"><strong>เงื่อนไขการใช้สตาร์บัคส์การ์ดอี-คูปองมูลค่า </strong> </span><span style="font-size:10pt;">
            <asp:Label ID="Label3" runat="server" style="font-weight: 700" Text=""></asp:Label>
             <strong>&nbsp;บาท</strong></span></p><br />
        <p style="font-size:11pt;margin:0;" class="text-left">
            1<span style="font-size:10pt;">.<span lang="th">สามารถใช้สิทธิ์ได้ที่ร้านสตาร์บัคส์ทุกสาขาในประเทศไทยเท่านั้น</span><span> โดยไม่มีวันหมดอายุ</span></span></p>

 

        <p style="font-size:11pt;margin:0;" class="text-left">
            2<span style="font-size:10pt;">.<span lang="th">สามารถใช้สิทธิ์โดยการแสดง Barcode ผ่านหน้าจอโทรศัพท์มือถือเท่านั้น</span></span></p>


        <p style="font-size:11pt;margin:0;" class="text-left">
            3<span style="font-size:10pt;">.<span lang="th">สามารถนำมาใช้ซ้ำได้จนกว่ายอดเงินในโค้ดจะหมดไป</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            4<span style="font-size:10pt;">.<span lang="th">ไม่สามารถทำการเปลี่ยนเป็นเงินสด หรือโอนยอดเงินไปสู่บัตรสตาร์บัคส์ได้</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            5<span style="font-size:10pt;">.<span lang="th">สามารถใช้ร่วมกับโปรโมชั่นส่งเสริมการขายของทางร้านได้</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            6<span style="font-size:10pt;">.<span lang="th">ในกรณีที่โค้ดถูกขโมย ทำลาย หรือนำไปใช้โดยไม่ได้รับอนุญาตจากเจ้าของโค้ด บริษัทไม่สามารถทำการคืนเงินในทุกกรณี</span></span></p>

             <p style="font-size:11pt;margin:0;" class="text-left">
            7<span style="font-size:10pt;">.<span lang="th">หากโค้ดชำรุดหรือมีการเปลี่ยนแปลงข้อมูลใดๆ ทางร้านมีสิทธิ์ปฏิเสธงดให้บริการ</span></span></p>

             <p style="font-size:11pt;margin:0;" class="text-left">
8<span style="font-size:10pt;">.<span lang="th">หากสินค้ามีมูลค่าสูงเกินยอดเงินในโค้ด ลูกค้าจะต้องชำระส่วนที่เหลือด้วยเงินสด หรือบัตรเครดิต หรือวิธีชำระเงินอื่นๆ</span></span></p>
    </td></tr>


       <tr><td class="text-center">
           <br />
           <asp:Button ID="Button1" runat="server" Text="กดใช้สิทธิ์" OnClick="Button1_Click" Height="50px" Width="150px" CssClass="button"/>
    </td></tr>


       <tr><td>
    </td></tr><tr><td>&nbsp;</td></tr></table></asp:Panel>

    <asp:Panel ID="Panel2" runat="server" Visible="false"><table style="width:90%;"><tr><td class="text-center"><center>
     <asp:Image ID="Image3" runat="server" Width="100%" /> </center></td></tr>
        <tr><td>&nbsp;<br /></td></tr>
          <tr><td class="text-center"><center>
              <asp:Image ID="Image1" runat="server" Width="100%" /></center>
    </td></tr>
        <tr><td class="text-center"><center>
            <br />
            โปรดแสดงบาร์โค้ดนี้ ณ จุดรับชำระเงิน<br />
            <br />
            โค้ดไม่มีวันหมดอายุ </center></td></tr>


     <tr><td>&nbsp;</td></tr></table></asp:Panel>




   </center>
        </div>
    
</asp:Content>

