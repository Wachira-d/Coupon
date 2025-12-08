<%@ Page Title="CENTRAL Gift e-voucher" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Central.aspx.cs" Inherits="Coupon.Central" %>

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
  background-color: #af1e2b; 
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
    <asp:Panel ID="Panel1" runat="server">
        <center><table style="width:90%;"><tr><td> <table style="width:100%;"><tr><td></td><td>&nbsp;</td><td>&nbsp;</td></tr>
        <tr>
            
            <td style="text-align: center;align-content:center">               <asp:Image ID="Image2" runat="server" Width="90%" ImageAlign="Middle" />
            </td>
            
        </tr>
        <tr>
            
            <td style="text-align: center;align-content:center;align-items:center"> <img src="./Images/Central/Logo.png" alt="" width="90%"  /></td>
            
        </tr>
        
        </table>
            </center>
                                              </td></tr><tr><td style="align-items:center;align-content:center; class="text-center"><center>
            &nbsp;</center></td></tr><tr><td class="text-center">
            <br />
            <br /> </td></tr><tr><td>
        <p style="font-size:11pt;margin:0;" class="text-left">
            <span lang="th" style="font-size:10pt;"><strong>เงื่อนไขการใช้ CENTRAL Gift e-voucher มูลค่า </strong> </span><span style="font-size:10pt;">
            <asp:Label ID="Label3" runat="server" style="font-weight: 700" Text=""></asp:Label>
             <strong>&nbsp;บาท</strong></span></p><br />
        <p style="font-size:11pt;margin:0;" class="text-left">
            1<span style="font-size:10pt;">.<span lang="th">กรุณาแสดงบาร์โค้ด e-Voucher นี้ให้กับพนักงานแคชเชียร์ ก่อนทำการชำระค่าสินค้า</span></span></p>

 

        <p style="font-size:11pt;margin:0;" class="text-left">
            2<span style="font-size:10pt;">.<span lang="th">e-Voucher นี้ใช้ได้ที่ ร้านค้าในเครือเซ็นทรัลกรุ๊ป เช่น เซ็นทรัล, เซน, มาร์คแอนด์สเปนเซอร์, มูจิ, เซ็นทรัล ฟู้ดฮอลล์, ท็อปส์, ซูเปอร์คุ้ม, อีทไทยโซนตลาด, เพาเวอร์บาย, ซูเปอร์สปอร์ต, บีทูเอส, โรบินสัน, ไทวัสดุ, บ้านแอนด์บียอน, มัทสึโมโตะ คิโยชิ และ ออฟฟิศเมท ตามมูลค่าที่แสดง (ยกเว้นร้านค่าเช่าพื้นที่, พื้นที่ศูนย์อาหาร) </span></span></p>


        <p style="font-size:11pt;margin:0;" class="text-left">
            3<span style="font-size:10pt;">.<span lang="th">e-Voucher นี้ไม่สามารถใช้ชำระค่าสินค้าประเภทบัตรโทรศัพท์, บัตรเติมเงินทุกประเภท, บัตรเงินสด, บัตรกำนัล, บัตรของขวัญ, สินค้าขายส่งและสินค้ากลุ่มโครงสร้างบางรายการ
</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            4<span style="font-size:10pt;">.<span lang="th">e-Voucher นี้ไม่สามารถใช้ชำระค่าสินค้าหรือบริการอันเป็นการฝ่าฝืนข้อห้าม หรือข้อกำหนดของกฏหมาย (ห้ามส่งเสริมการขาย) ซึ่งบังคับใช้อยู่ในปัจจุบัน หรืออาจจะมีขึ้นในอนาคตได้ เช่น สุรา, บุหรี่, ยา, อาหารสำหรับทารก อาหารสำหรับเด็กเล็กและอาหารเสริมสำหรับทารก</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            5<span style="font-size:10pt;">.<span lang="th">e-Voucher นี้ขอสงวนสิทธิ์ในการรับ/การคืน การขอยกเลิก และชดเชยความเสียหายกรณี e-Voucher หมดเวลา หรือมีการเปลี่ยนแปลงที่เกิดขึ้น</span></span></p>



        <p style="font-size:11pt;margin:0;" class="text-left">
            6<span style="font-size:10pt;">.<span lang="th">ไม่สามารถเปลี่ยนหรือทอนเป็นเงินสดได้</span></span></p>

             <p style="font-size:11pt;margin:0;" class="text-left">
            7<span style="font-size:10pt;">.<span lang="th">e-Voucher มีอายุ 5 ปี นับแต่วันที่สั่งซื้อ</span></span></p>

             <p style="font-size:11pt;margin:0;" class="text-left">
8<span style="font-size:10pt;">.<span lang="th">ขอสงวนสิทธิ์ในการเปลี่ยนแปลงเงื่อนไขโดยไม่ต้องแจ้งให้ทราบล่วงหน้า</span></span></p>
    </td></tr>


       <tr><td class="text-center" style="align-items:center;align-content:center;text-align:center">
           <br /><center>
           <asp:Button ID="Button1" runat="server" Text="กดใช้สิทธิ์" OnClick="Button1_Click" Height="50px" Width="150px" CssClass="button" /></center>
    </td></tr>


       <tr><td>
    </td></tr><tr><td>&nbsp;</td></tr></table></asp:Panel>

    <asp:Panel ID="Panel2" runat="server" Visible="false"><table style="width:90%;"><tr><td class="text-center"><center>
     <asp:Image ID="Image3" runat="server" Width="100%" /> </center></td></tr>
        <tr><td>&nbsp;<br /></td></tr>
          <tr><td class="text-center"><center>
              <asp:Image ID="Image1" runat="server" Width="100%" /></center>
    </td></tr>
        <tr><td class="text-left">
            <br />
            <b>เงื่อนไขการใช้</b><br />
            <br />
            ใช้ได้ที่ เซ็นทรัล, มาร์คแอนด์สเปนเซอร์, มูจิ, เซ็นทรัล ฟู้ดฮอลล์, ท็อปส์, ซูเปอร์คุ้ม, อีทไทยโซนซูเปอร์มาร์เก็ต, Thai Favourites, เพาเวอร์บาย, ซูเปอร์สปอร์ต, บีทูเอส, โรบินสัน, ไทวัสดุ, บ้านแอนด์บียอน, มัทสึโมโตะ คิโยชิ, ออฟฟิศเมท และ ออโต้วัน (ยกเว้นร้านค่าเช่าพื้นที่, พื้นที่ศูนย์อาหาร และช่องทางออนไลน์) สอบถามรายละเอียดเพิ่มเติม กรุณาติดต่อ MAGIC Gift Card Call Center โทร 02 100 8777 จ – ศ 9:00 - 20:00 น. / ส – อา 10:30 - 20:00 น. หรือส่งอีเมล มาที่ cpmsupport@central.co.th </td></tr>


     <tr><td>&nbsp;</td></tr></table></asp:Panel>




   </center>
        </div>
    
</asp:Content>

