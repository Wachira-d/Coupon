<%@ Page Title="Lotus e-voucher" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Lotus.aspx.cs" Inherits="Coupon.Lotus" %>

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
  background-color: #00bdb5; /* Green */
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
    <asp:Panel ID="Panel1" runat="server"><table style="width:90%;"><tr><td> <table style="width:100%;"><tr><td style="align-items:start;align-content:start;"><img src="./Images/Lotus/Logo.png" alt="" width="40%" class="navbar-header" /></td><td>&nbsp;</td><td>&nbsp;</td></tr>
        <tr>
            <td style="align-items:start;align-content:start;">
                <asp:Image ID="Image2" runat="server" Width="100%" />
            </td>
            <td>&nbsp;</td>
            <td>&nbsp;</td>
        </tr>
        <center>
        </table></td></tr><tr><td style="align-items:center;align-content:center; class="text-center"><center>
                    <br />
วันหมดอายุ :
<asp:Label ID="Label1" runat="server" Text=""></asp:Label>
<br />
เวลา 23.59 น.<br /></center></td></tr><tr><td class="text-center">
            <br />
 </td></tr><tr><td>
        <p style="font-size:11pt;margin:0;" class="text-left">
            <span lang="th" style="font-size:10pt;"><strong>เงื่อนไขการใช้อี-คูปองบัตรกำนัลโลตัสมูลค่า </strong> </span><span style="font-size:10pt;">
            <asp:Label ID="Label3" runat="server" style="font-weight: 700" Text=""></asp:Label>
            <span lang="th"><strong>&nbsp;บาท จากบริษัท ฮอนด้า ออโตโมบิล (ประเทศไทย) จำกัด</strong></span></span></p>
        <p style="font-size:11pt;margin:0;" class="text-left">
            1<span style="font-size:10pt;">.<span lang="th">สามารถใช้ซื้อสินค้าที่โลตัส ทุกสาขา และต้องใช้ก่อนวันหมดอายุ</span></span></p>

 

        <p style="font-size:11pt;margin:0;" class="text-left">
            2<span style="font-size:10pt;">.<span lang="th">E-coupons ไม่สามารถถอน เปลี่ยนเป็นเงินสด หรือรูปแบบการชำระอื่น และไม่สามารถทอนส่วนต่างเป็นเงินสด กรณีซื้อของน้อยกว่ามูลค่าของ E-coupons</span></span></p>


        <p style="font-size:11pt;margin:0;" class="text-left">
            3<span style="font-size:10pt;">.<span lang="th">ไม่สามารถใช้ซื้อสินค้าประเภทเครื่องดื่มแอลกอฮอล์ บุหรี่ บัตรเติมเงิน บัตรของขวัญ นมดัดแปลงสำหรับทารกและนมดัดแปลงสูตรต่อเนื่องสำหรับทารกและเด็ก (สูตร </span>1<span lang="th"> และ </span>2 ) Bill payment <span lang="th">และค่าธรรมเนียม บริการไปรษณีย์ ศูนย์อาหาร โลตัสช้อปออนไลน์ และ สินค้า/ร้านค้าในบริเวณพื้นที่เช่าของโลตัส</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            4<span style="font-size:10pt;">.<span lang="th">การใช้ </span>E-coupons <span lang="th">จะต้องกดรับสิทธิ์ที่หน้าเคาน์เตอร์แคชเชียร์เท่านั้น ขอสงวนสิทธิ์ไม่ให้ใช้รูปภาพที่มีการบันทึกเก็บไว้ หรือ พิมพ์บนกระดาษ</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            5<span style="font-size:10pt;">.<span lang="th">ส่วนลดจากการใช้ จะไม่ถูกนำมาคำนวนเป็นแต้มคลับการ์ด</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            6<span style="font-size:10pt;">.<span lang="th">บริษัทฯ ขอสงวนสิทธิ์ในการเปลี่ยนแปลงเงื่อนไขโดยไม่จำเป็นต้องแจ้งให้ทราบล่วงหน้า</span></span></p>

             <p style="font-size:11pt;margin:0;" class="text-left">
            7<span style="font-size:10pt;">.<span lang="th">หากพบปัญหาในการแสกน barcode ที่เคาน์เตอร์ สามารถแจ้งทางสาขาให้กด “ทดแทนกิ๊ฟการ์ด” หรือ “ทดแทนสมาร์ทคูปอง” ให้ตามมูลค่าคูปองได้เลยค่ะ</span></span></p>
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
            วันหมดอายุ :
        <asp:Label ID="Label2" runat="server" Text=""></asp:Label>
        <br />
        เวลา 23.59 น.</center></td></tr>


     <tr><td>&nbsp;</td></tr></table></asp:Panel>




   </center>
        </div>
    
</asp:Content>
