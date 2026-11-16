# -*- coding: utf-8 -*-
"""按原版 BAML 文案表还原所有界面标签"""
import io, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

def apply(path, pairs):
    s = open(path, encoding='utf-8').read()
    n = 0
    for old, new in pairs:
        if old in s:
            s = s.replace(old, new)
            n += 1
        else:
            print('  MISS:', path, '|', old[:60])
    open(path, 'w', encoding='utf-8').write(s)
    print('%s: %d/%d applied' % (path.split('/')[-1], n, len(pairs)))

apply('MaoDuoDuo/MainWindow.xaml', [
    ('<TextBlock Text="铺货模式："', '<TextBlock Text="操作类型："'),
    ('<TextBlock Text="服务类型："', '<TextBlock Text="白底图："'),
    ('<ComboBox x:Name="box_type6" Width="120" Height="30"/>',
     '<ComboBox x:Name="box_type6" Width="120" Height="30" SelectedIndex="0"><ComboBoxItem Content="不处理"/><ComboBoxItem Content="生成白底图"/></ComboBox>'),
    ('<TextBlock Text="上传位置："', '<TextBlock Text="上传到："'),
    ('<TextBlock Text="店铺分类："', '<TextBlock Text="商品分类："'),
    ('Watermark="如:食品饮料>零食/坚果/特产"', 'Watermark="数码电器>3C数码配件>手机配件>手机贴膜"'),
    ('Content="使用cate.json自动分类"', 'Content="自动分类"'),
    ('<TextBlock Text="主图处理："', '<TextBlock Text="主图设置："'),
    ('<TextBlock Text="详情图裁剪："', '<TextBlock Text="删除详情前："'),
    ('<TextBlock Text=" 至 "', '<TextBlock Text="删除详情后："'),
    ('<TextBlock Text="预售方式："', '<TextBlock Text="是否预售："'),
    ('<TextBlock Text="发货时效："', '<TextBlock Text="发货时间："'),
    ('<TextBlock Text="品牌处理："', '<TextBlock Text="品牌属性："'),
    ('<TextBlock Text="库存方式："', '<TextBlock Text="库存设置："'),
    ('<TextBlock Text="免审库存："', '<TextBlock Text="最低价库存："'),
    ('<TextBlock Text="低于此数库存置0："', '<TextBlock Text="库存低于："'),
    ('<TextBlock Text="货号处理："', '<TextBlock Text="编号："'),
    ('<TextBlock Text="SKU编号："', '<TextBlock Text="商品编码："'),
    ('<TextBlock Text="SKU前缀："', '<TextBlock Text="前缀："'),
    ('<TextBlock Text="SKU后缀："', '<TextBlock Text="后缀："'),
    ('<TextBlock Text="图片下载目录："', '<TextBlock Text="保存路径："'),
    ('<TextBlock Text="图片加工："', '<TextBlock Text="图片处理："'),
    ('Content="裂变图设置"', 'Content="设置"'),
    ('Content="标题前缀"', 'Content="加前缀"'),
    ('Content="标题后缀"', 'Content="加后缀"'),
    ('Title="标题替换（每行: 旧=新）"', 'Title="替换标题（每行: 旧=新）"'),
    ('Content="SKU名称同步替换"', 'Content="同时替换SKU"'),
    ('Title="屏蔽关键词（每行一个）"', 'Title="过滤关键词（每行一个）"'),
    ('Content="SKU匹配关键词"', 'Content="同时过滤SKU"'),
    ('<TextBlock Text="价格处理："', '<TextBlock Text="商品改价："'),
    ('<TextBlock Text="裂变公式："', '<TextBlock Text="裂变价格公式："'),
    ('Title="属性复制（每行: 源属性=目标属性;目标属性2）"', 'Title="属性匹配（每行: 源属性=目标属性;目标属性2）"'),
    ('Title="属性补全（每行: 属性=值1;值2）"', 'Title="属性填充（每行: 属性=值1;值2）"'),
    ('Content="存在即覆盖"', 'Content="强制替换商品属性"'),
    ('Content="开始铺货"', 'Content="开始上传"'),
    ('Content="终止"', 'Content="停止"'),
    ('Content="重新登录买手号"', 'Content="登录买手号"'),
    ('Content="售后登录"', 'Content="扫码商户"'),
    ('Title="已完成（自动记录）"', 'Title="完成商品ID"'),
    ('Content="跳过已完成商品"', 'Content="跳过商品编辑"'),
    ('Content="测试模式（不自动跳过异常页）"', 'Content="获取商品失败后使用测试数据"'),
    ('Content="活动报名(KDC)"', 'Content="一键卡大促"'),
    ('Content="活动报名2"', 'Content="仅退款申诉2"'),
    ('Content="评价采集测试"', 'Content="卡反拼单"'),
    ('Content="加速/补流量"', 'Content="申诉理由"'),
    ('Content="活动报名(KDC-2)"', 'Content="仅退款申诉"'),
    ('Content="ROI计算器"', 'Content="盈亏计算"'),
    ('Header="原商品ID"', 'Header="原商品ID"'),
    ('Header="新商品ID"', 'Header="上传后ID"'),
    ('Header="结果"', 'Header="上传结果"'),
    ('<MenuItem Header="删除选中"', '<MenuItem Header="删除勾选"'),
    ('<MenuItem Header="清空记录"', '<MenuItem Header="删除所有"'),
    ('<TextBlock Text="异常记录"', '<TextBlock Text="上传失败"'),
    ('<TextBlock x:Name="txt_validity"', '<TextBlock Text="有效期至：" FontSize="12" Foreground="#FFE" VerticalAlignment="Center"/>\n                    <TextBlock x:Name="txt_validity"'),
    ('Header="前端采集页"', 'Header="买手页"'),
    ('Header="商家发布页"', 'Header="商户页"'),
    # 行布局防裁剪：横向 StackPanel → WrapPanel（仅设置区各行）
    ('<StackPanel Orientation="Horizontal" Margin="2,0,0,4">', '<WrapPanel Margin="2,0,0,4">'),
    ('<StackPanel Orientation="Horizontal" Margin="2,4">', '<WrapPanel Margin="2,4">'),
    ('<StackPanel Orientation="Horizontal" Margin="2,10">', '<WrapPanel Margin="2,10">'),
])

apply('MaoDuoDuo/MainWindow.xaml', [
    # WrapPanel 后遗留的 </StackPanel> 对应闭合标签需同步替换（前 3 个设置行）
])

# 修正 MainWindow.xaml 中因替换产生的闭合标签错位：把设置区行内 </StackPanel> 换成 </WrapPanel>
s = open('MaoDuoDuo/MainWindow.xaml', encoding='utf-8').read()
# 仅处理 WrapPanel 开始后紧跟的第一个 </StackPanel>
import re
def fix_close(m):
    return m.group(0).replace('</StackPanel>', '</WrapPanel>')
s = re.sub(r'<WrapPanel[^>]*>(?:(?!</WrapPanel>|<WrapPanel|</StackPanel>).)*?</StackPanel>', fix_close, s, flags=re.S)
open('MaoDuoDuo/MainWindow.xaml', 'w', encoding='utf-8').write(s)
print('WrapPanel close tags fixed')

apply('DuoDuoPuHuo.Views/Compute.xaml', [
    ('Title="ROI 计算器"', 'Title="盈亏计算"'),
    ('<TextBlock Text="推广 ROI 计算器"', '<TextBlock Text="盈亏计算"'),
    ('<TextBlock Text="实际售价："', '<TextBlock Text="销售价格："'),
    ('<TextBlock Text="成本价格："', '<TextBlock Text="产品成本："'),
    ('<TextBlock Text="服务宽度(%)："', '<TextBlock Text="服务扣点(%)："'),
    ('<TextBlock Text="主图成本："', '<TextBlock Text="赠品成本："'),
    ('Content="计算 ROI"', 'Content="计算ROI"'),
    ('<TextBlock Text="  ROI = "', '<TextBlock Text="实际投产ROI = "'),
    ('<TextBlock Text="上架花费："', '<TextBlock Text="实际花费："'),
    ('<TextBlock Text="上架推车："', '<TextBlock Text="实际投产："'),
    ('<TextBlock Text="霸屏推车："', '<TextBlock Text="保本投产："'),
    ('Content="计算推广盈利"', 'Content="计算推广盈亏"'),
    ('<TextBlock Text="  盈利 = "', '<TextBlock Text="推广盈亏 = "'),
])

apply('MaoDuoDuo.Views/DRS.xaml', [
    ('<TextBlock Text="点单数一："', '<TextBlock Text="当前DSR："'),
    ('<TextBlock Text="评价数："', '<TextBlock Text="当前评价数量："'),
    ('<TextBlock Text="点单数二："', '<TextBlock Text="目标DSR："'),
    ('<TextBlock Text="微信数："', '<TextBlock Text="需全五星单量："'),
    ('<TextBlock Text="计算类型："', '<TextBlock Text="选择类型："'),
    ('<TextBlock Text="产品成本："', '<TextBlock Text="输入产品成本："'),
    ('<TextBlock Text="产品售价："', '<TextBlock Text="输入产品售价："'),
    ('<ui:FlatListView x:Name="CpTcList" Grid.Row="1" Margin="2,8"/>',
     '''<ui:FlatListView x:Name="CpTcList" Grid.Row="1" Margin="2,8">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="产品类型" DisplayMemberBinding="{Binding ProductType}" Width="110"/>
                    <GridViewColumn Header="产品售价" DisplayMemberBinding="{Binding ProductPrice}" Width="90"/>
                    <GridViewColumn Header="利润" DisplayMemberBinding="{Binding ProductProfit}" Width="90"/>
                    <GridViewColumn Header="毛利率" DisplayMemberBinding="{Binding ProductGPM}" Width="90"/>
                    <GridViewColumn Header="目标投产" DisplayMemberBinding="{Binding ProductNum}" Width="90"/>
                    <GridViewColumn Header="操作" Width="60">
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <Button Content="删除" Click="btnDelete_Click" Cursor="Hand"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                </GridView>
            </ListView.View>
        </ui:FlatListView>'''),
])

apply('MaoDuoDuo.Views/ImageSet.xaml', [
    ('Title="图片加工设置"', 'Title="图片设置"'),
    ('<TextBlock Text="图片底图处理"', '<TextBlock Text="底图选择"'),
    ('Content="主图进行图中图处理"', 'Content="主图"'),
    ('Content="SKU图进行图中图处理"', 'Content="图片"'),
])

apply('DuoDuoPuHuo.Views/LYSet.xaml', [
    ('Title="申诉理由设置"', 'Title="自定义申诉理由"'),
    ('<TextBlock Text="申诉理由设置（每行一个）"', '<TextBlock Text="自定义申诉理由（回车换行算一条）"'),
    ('Content="保存"', 'Content="保存理由"'),
])

apply('MaoDuoDuo.Views/Home.xaml', [
    ('Watermark="输入商品关键词"', 'Watermark="请输入关键词或商品ID"'),
    ('Content="搜索商品"', 'Content="搜索"'),
    ('Watermark="输入店铺名称"', 'Watermark="请输入关键词或店铺ID"'),
    ('Content="搜索店铺"', 'Content="搜索"'),
    ('<TextBlock Text="热搜词"', '<TextBlock Text="平台热搜"'),
    ('<TextBlock Text="快捷入口"', '<TextBlock Text="入口直达"'),
])

apply('MaoDuoDuo.Views/Hot.xaml', [
    ('Content="常规筛选"', 'Content="正常"'),
    ('Content="排序筛选"', 'Content="排序查看"'),
])

apply('MaoDuoDuo.Views/VideoCapture.xaml', [
    ('<TextBlock Text="抖音关键词："', '<TextBlock Text="关键词采集："'),
    ('Content="采集视频"', 'Content="开始采集"'),
    ('Content="正序排列"', 'Content="正序"'),
    ('Content="倒序排列"', 'Content="倒序"'),
    ('Header="作者"', 'Header="作者名称"'),
    ('Header="点赞数"', 'Header="喜欢人数"'),
    ('<TextBlock Text="下载目录："', '<TextBlock Text="设置保存位置："'),
])

apply('MaoDuoDuo.Views/ProductReviewsDownload.xaml', [
    ('Title="评价下载"', 'Title="评论下载"'),
    ('Content="开始下载评价"', 'Content="下载"'),
    ('<TextBlock Text="下载条数："', '<TextBlock Text="下载数量："'),
    ('<TextBlock Text="已下载："', '<TextBlock Text="已下载数："'),
])

apply('MaoDuoDuo.Views/ProductShow.xaml', [
    ('Title="商品信息"', 'Title="信息展示"'),
    ('<TextBlock Text="商品标题："', '<TextBlock Text="商品标题："'),
    ('<TextBlock Text="销量信息："', '<TextBlock Text="销售信息："'),
    ('<TextBlock Text="店铺名称："', '<TextBlock Text="店名："'),
    ('<TextBlock Text="店铺DSR："', '<TextBlock Text="店铺DSR："'),
    ('Content="复制店铺ID"', 'Content="复制店铺ID"'),
    ('<TextBlock Text="店铺销量："', '<TextBlock Text="销售信息："'),
])

apply('MaoDuoDuo.Views/DownWindow.xaml', [
    ('Title="下载进度"', 'Title="文件下载"'),
])

apply('MaoDuoDuo/LoginWindow.xaml', [
    ('Content="记住账号密码"', 'Content="记住密码"'),
    ('Content="付费/续费"', 'Content="充值"'),
    ('Content="忘记密码"', 'Content="修改密码"'),
])

apply('MaoDuoDuo.Views/ShopSet.xaml', [
    ('Content="获取店铺登录二维码"', 'Content="获取二维码"'),
    ('Content="检查登录状态"', 'Content="统一更新cookie"'),
    ('Content="查询"', 'Content="查找店铺"'),
    ('Content="全选/取消"', 'Content="全选"'),
    ('Content="清空店铺"', 'Content="删除失效店铺"'),
    ('Header="手机号"', 'Header="店铺账号"'),
    ('Header="主营类目"', 'Header="归属类目"'),
])
print('ALL DONE')
