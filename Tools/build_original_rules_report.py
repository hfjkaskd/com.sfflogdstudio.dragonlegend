from pathlib import Path
import json, re, csv, io, base64, hashlib, html, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
OUT=Path('C:/Users/pc/Downloads')
AUDIT=Path('C:/Projects/Nut Sort Relax/reconstruction/mumu-current')
esc=lambda v:html.escape(str(v))
cfg=json.loads((ROOT/'Assets/StreamingAssets/RecoveredConfig/Remote/cp_test.json').read_text(encoding='utf-8-sig'))
organic=json.loads((AUDIT/'config/GoldenDragon_organic.json').read_text(encoding='utf-8-sig'))
rules=[];cases=[]
def rule(module,title,body,evidence,checks):
    rid=f'R{len(rules)+1:02}'
    rules.append(dict(id=rid,module=module,title=title,body=body,evidence=evidence))
    for pre,action,expected in checks:
        cases.append(dict(id=f'TC{len(cases)+1:03}',rule=rid,module=module,priority='P0' if module in ['提现','广告','次数'] else 'P1',precondition=pre,steps=action,expected=expected,status='待原版执行'))

rule('配置','非自然量不是另一套安装包','原版 LoadConfig 根据输入、IsA 和 backup 状态选择配置。非空远程输入可进入解码；本地分支组合 GoldenDragon / GoldenDragonA 与 _default / _organic。不能从国家或安装渠道名称直接推导最终配置。','config-selection-native.md',[
('原版未修改包，保存配置缓存与运行日志','分别测试非空远程输入和离线启动','记录实际采用的配置来源、Ripg 与哈希；不以页面外观替代来源证据'),
('MuMu #0 已应用本地非自然量覆盖','冷启动两次，并与未修改样本对照','覆盖样本保留 default；只证明本地覆盖有效，不证明原版服务器分流')])
rule('次数','初始次数、上限和广告补充','cp_test：初始 20 次、存储上限 30 次、额外补充 +10；MojGping 第二项 999 是累计次数门槛。初始次数不等于恢复后或旧存档显示的次数。','more-spin-window.md',[
('新建独立存档，default 配置','首次进入主界面','初始次数读取 InirGping=20；记录初始化及恢复是否随后改变次数'),
('SPIN=0','点击 Spin，再点击 GET NOW，完成奖励回调','先打开 MORE SPINS，不消耗次数；成功后增加 10，重复回调不重复领取'),
('SPIN=25','完成一次 +10 奖励','存储次数封顶 30；区分 setter 的事件参数和实际存储值')])
rule('次数','自动恢复读取时间余数','仅 default 且未满时恢复。整段时间折算新增次数，保留余数；每级恢复秒数由 GpinQP 决定。第 1–9 级为 30/60/90/90/90/120/180/240/300 秒，9 级及以上读取末项。','spin-recovery.md',[
('default；Level=1；SPIN=10；离线 65 秒','重开主界面','新增 2 次，余 5 秒；下一次恢复剩 25 秒'),
('organic；相同次数与离线时间','重开主界面','不走 default 自动恢复分支'),
('default；次数已满','等待多个周期','不超过上限；满额停止恢复计时')])
rule('次数','累计门槛并非通用每日领取上限','非 default 在 LimitSpinCount 达到配置门槛时不请求 extraspin 广告；default 豁免这个限制。现有证据不能把它描述为每日 N 次广告限制。','more-spin-window.md',[
('organic；累计次数=门槛−1','请求额外次数','允许进入广告请求'),
('organic；累计次数=门槛','请求额外次数','出现限制提示，无广告请求；核对原版点击锁保留行为'),
('default；累计次数=999','请求额外次数','不受这一非 default 门槛阻断')])
rule('玩法','有效转动才扣次数','忙碌或结果生成中拒绝再次开始；次数为零打开补充窗口。有效转动扣 1 次，并推进经验、Jackpot 累加和福袋计数。普通转动不等于每日任务里的 Slots 小游戏。','spin-input-routing.md',[
('有次数且空闲','快速连点 Spin','只接受一次转动，扣 1 次；忙碌点击不产生第二轮'),
('SPIN=0','点击 Spin','次数、经验及转轮状态不被扣减或推进')])
rule('奖励','大奖判定和累加公式','Big Win 阈值按配置逆序检查 amount >= threshold × bet；default 阈值为 2/4/6。Jackpot 原始金额为 multiplier × bet + JpOpp × addCount，货币显示再除以 100。','jackpot-flow.md',[
('固定 bet，使用可控结算输入','分别检查阈值−最小单位、等于阈值、阈值+最小单位','等于阈值应命中；高档优先。黑盒无法控制时标记阻塞并记录实际样本'),
('default；JpOpp=10；固定 multiplier、bet、addCount','结算一次 Jackpot','原始金额符合公式，显示金额=原始值/100；不把显示四舍五入写回存档')])
rule('奖励','福袋计数与随机奖励','default 每 12 次计数触发福袋。权重 500/300/200 对应原始奖励区间 2000–2500 / 2500–3000 / 3000–5000，整数上界包含；界面美元除以 100。','bank-selection.md',[
('福袋计数=11','完成下一次有效转动及对应流程','满足 12 次触发条件；弹窗与结算只发生一次'),
('独立样本、固定配置','记录多次福袋结果及领取分支','基础奖励位于配置区间；倍率另按领取分支核对，单次样本不证明权重比例')])
rule('Bonus','牌组各列共享一次随机行','同一个随机行读取 Ltoo/Qoi/Jin/Roo/Rgkorp，按 1/2/3/4/0 展开并洗牌。不是分别独立抽五个数量，Reward 金额在牌动画阶段另行抽取。','bonus-round.md',[
('可读取隐藏牌组的独立测试样本','多次生成牌组，记录各类数量','每组数量对应同一配置行；不得拼接来自不同配置行的数量'),
('已生成牌组','重复点击同一张及点击另一张','已揭示位置不重复消费；新点击不重新生成整个牌组')])
rule('Bonus','Grand / Major / Minor 集齐规则','Grand=招财进宝；Major=宝宝宝；Minor=财财财。每张牌只消费第一个仍需该牌型的目标中的一个位置。','bonus-round.md',[
('Grand 尚缺财，Minor 也缺财','揭示一张财','仅消费一个 Jackpot 的一个目标，不同时推进两个奖池'),
('Grand 已用过财，Minor 未完成','继续揭示财','可继续匹配 Minor；集满对应模式才触发奖励')])
rule('Free','龙珠颜色选择概率分布','颜色映射配置列 0、1、其他→2；同列的 Slots / Wheel / Treasure / Lucky 四项权重决定小游戏。颜色不固定对应单一玩法。','free-minigame-rules.md',[
('三种 BallType，保存配置','各记录多次小游戏路由','按相应列取四项权重；不能用一次出现结果推断固定映射'),
('Slots 或 Lucky 奖励样本','核对取值上下界与调用顺序','Slots 先抽权重行再抽闭区间金额；Lucky 在其配置闭区间抽取')])
rule('Free','转盘八个扇区的映射','按索引依次是 Major、Cash、Mini、Cash、Grand、Cash、Mini、Cash。转盘奖励 getter 直接读对应配置，不再次消耗随机数。','free-minigame-rules.md',[
('原版转盘可观测','记录最终索引和奖项','索引 0/2/4/6 为 Major/Mini/Grand/Mini；其余为 Cash'),
('白盒规则测试，固定随机状态','读取同一格奖励两次','金额相同，随机流不推进；标记为静态/白盒验证，不冒充设备实测')])
rule('宝藏','收集记录和概率存在原版特殊行为','GetCollectInfos 按配置顺序建缓存；RandomCollectIndex 使用 Ip 列作为权重，不使用 Ronpom。首次新增记录 count=1，不按传入增量初始化。','treasure-collection.md',[
('无某宝藏记录；可控增量=5','触发新增记录','首次 count=1；后续未领取记录才按增量累加'),
('已有已领取记录','再次触发同 ID 收集','不追加其 count；核对存档仍走保存路径')])
rule('每日任务','六个任务的真实事件','1 Big Win、2 Jackpot、3 Free Game、4 Slots 小游戏、5 Bonus、6 登录。default 每项目标均为 1，基础奖励依次 $20/$30/$10/$8/$12/$5。','task-native-alignment.md',[
('每日任务 4 未完成','普通主转轮转动一次，再进入 Slots 小游戏','普通转动不完成任务 4；小游戏入口推进任务 4'),
('分别缺少六类任务记录','触发对应事件','只有匹配 ID 的任务推进，目标及基础奖励读取配置')])
rule('每日任务','领取先记录，再进入奖励表现','领取先保存 isRecieve，避免到账前重复领取；jump=0 直接分支与 UIRewardView 分支不同。2 倍 / 0.5 倍在奖励弹窗中应用，不应全局乘到所有任务。','task-native-alignment.md',[
('任务已完成未领取','快速重复点击 CLAIM','先保存领取状态；不会重复预约奖励'),
('选 jump=0 与 jump=1 的任务','分别普通领取、奖励广告领取','核对直领基础奖励与弹窗分支倍率；广告失败不伪造成功到账')])
rule('每日任务','default 按本地日期重置','登录先记录任务 6；本地日期前进且 Ripg=default 时清空并重新记录登录。任务页倒计时指向本地午夜；organic 不执行同样的每日清空。','task-native-alignment.md',[
('default；已有任务；跨本地午夜','重新登录','旧任务清空并记录登录任务；相同日期重进不重复清空'),
('organic；已有任务；跨本地午夜','重新登录','不套用 default 每日清空规则'),
('default；任务页距午夜很近','关闭任务页，跨过倒计时终点，再打开','核对全局计时清空与重开刷新；受 scaled time 行为约束')])
rule('广告','主动补充次数使用 extraspin','GET NOW 请求奖励广告，placement/scene 均为 extraspin。失败回调清点击锁以允许重试；成功读取实时次数并加配置增量。','more-spin-window.md',[
('MORE SPINS 弹窗','广告失败后再次点 GET NOW','弹窗可重试，失败不加次数'),
('广告请求未结束','重复点领取并尝试关闭','原版点击锁阻断重复请求；记录关闭行为'),
('广告播放期间次数被其他合法来源改变','完成广告','基于成功时的实时次数加值并封顶')])
rule('广告','插屏：等级、概率、冷却三层判断','IsA 直接返回；Level<=最低配置等级拒绝。随机整数 0..999 与概率做 <= 比较，随机在冷却检查前消耗。首次合格请求回填时间以允许初次展示；仅成功回调更新会话冷却。','interstitial-policy.md',[
('IsA 或 Level=1','触发插屏调用点','本分支不发起插屏'),
('default Level=2，概率=0；可控 RNG','分别返回随机 0 和 1','0 命中，1 不命中；0 不是严格零概率'),
('概率已命中；距上次成功 9/10 秒','各触发请求','9 秒被冷却阻断，10 秒满足边界；失败不重置成功时间')])
rule('广告','七个插屏调用点不等于七次实播','原版扫描确认 bank、big win、task reward、jackpot、network error、reward/lucky、treasure 七个调用点，统一 iv_close。Free-start 普通关闭没有该插屏调用。','interstitial-policy.md',[
('分别进入七个业务场景；满足策略条件','关闭或完成对应分支并记录 SDK 日志','区分调用、策略通过、请求、实际展示、成功回调五个阶段'),
('Free-start 普通关闭','关闭免费游戏介绍','不凭相似弹窗额外插入 iv_close 请求')])
rule('提现','金额门槛之后还有任务','default 档位为 $500/$1,000/$3,000/$5,000/$10,000。任务分支各档首阶段为转动 20/40/60/80/100 次；达到现金门槛不等于已完成后续审核。','withdrawal-tasks-integration.md',[
('余额比所选档位少 1 个原始单位','尝试申请','余额不足，不新建可执行提现任务'),
('达到现金门槛；独立测试账户/本地测试副本','确认账户后检查本地任务数据','本地任务分支展示对应 Spin 目标及等待时间；不得据此宣称实际付款')])
rule('提现','六个计数阶段与收集阶段','转动→广告→Jackpot→BigWin→Treasure→FreeGame；各阶段配置等待均为 86400 秒。后续失败/审核分支进入 step=6 收集宝藏。没有证据可将其描述为保证第七天到账。','withdrawal-tasks-integration.md',[
('某阶段次数未达标，等待已结束','点击 CONTINUE','仍阻断，不能仅按时间跳过任务'),
('次数达标，仍剩 1 秒','点击 CONTINUE，再在到期后点击','到期前阻断；次数及等待均满足后才推进并重置新阶段计数/起始时间'),
('FreeGame 阶段满足要求，isCashout=false','继续','进入宝藏收集阶段，不能直接写成功付款')])
rule('提现','成功与失败任务分支不能混用','目标读取取决于 isCashout；成功分支原版调用存在 index/step 参数交换特征，保留证据而不是按名称猜测。step=1000 才进入订单处理路径。','cash-bottom-conditions.md',[
('同一档位分别构造两种 isCashout','读取当前任务并推进','分别读取 success/fail 配置，不共用同一目标表'),
('某些后续目标为 0','推进任务','跳过相应零目标阶段；逐项核对原版参数顺序')])
rule('提现','重开与后台恢复以保存时间核对','任务记录含 id/type/step/count/time/isCashout。必须记录重开前后的完整字段，区分展示计时、实际 UTC 截止时间和本地模拟迁移。','application-focus.md',[
('已有任务与明确保存时间','后台等待后重开提现页','进度保留、剩余时间按原版生命周期刷新；不重新开始 24 小时'),
('复刻旧存档 step=1000 的本地 pending 订单','运行迁移并重开两次','仅复刻兼容测试：迁移一次且不清余额；该迁移不是原版规则')])
rule('显示','现金展示与存储单位要分开','CurrencyUtils.FormatCurrency 将原始值除以 100，EN 使用美元格式，其他语言值走 BR 格式分支。格式化不等于写回余额，也不代表实际付款币种。','bank-progress.md',[
('原始金额 1234.56，EN','刷新界面','显示 $12.35；原始值不被改写为 1235'),
('同一原始金额，切换语言参数','对照 EN 与非 EN','验证符号及小数格式，不推断服务器地区分配')])
rule('显示','遮罩保留底图并让特效可见','以原版画面为外观验收基准：普通图标在暗色遮罩下可辨认，特殊龙珠、金币及火焰的层级需逐一对照。复刻的 sortingOrder 数值不是原版业务配置。','free-specials-clipping.md',[
('免费游戏存在普通图标与龙珠','截图对照同一显示阶段','底图可辨认；特殊奖励不被自己的遮罩覆盖'),
('火焰横跨多列且有弹窗','逐帧查看层级与裁剪','火焰无方块切断，弹窗按钮不被无关遮罩拦截')])

rule('设置与帮助','帮助翻页与设置保存','原版帮助共三页，索引 0..2 循环，重新打开回到第 0 页。Music 修改 IsMusic 并立即更新背景音乐；名为 Sound 的对象实际修改 IsVibrate，不能视作音效开关。','main-utility-integration.md',[
('帮助页打开','向前翻三次，再关闭重开','三页循环；重开回第 0 页'),
('背景音乐开启','关闭 Music，再重启游戏','背景音乐立即停止，设置保持关闭'),
('设置页打开','切换手机图标的开关并重开','保存 IsVibrate；开关保存与设备实际震动需分别验证')])
rule('设置与帮助','设置内的导航入口','HOW TO PLAY 关闭设置并进入帮助；TERM OF USE 打开可滚动条款页；CONTACT US 打开邮件编辑器，不自动发送。现有记录未验证设备邮件应用实际启动。','main-utility-integration.md',[
('设置页打开','依次打开 HOW TO PLAY、TERM OF USE 并返回','帮助及条款正常打开，条款能滚动，关闭后主界面可交互'),
('安装邮件客户端的原版测试设备','点击 CONTACT US','打开邮件编辑器并核对收件人；不自动发送，未配置客户端的情况单独记录')])

cases[1]['status']='待修改样本执行'
cases[50]['status']='待复刻兼容执行'

# Machine-readable evidence and test inventories are provenance, not fresh execution.
evidence_names=sorted({r['evidence'] for r in rules}|{'main-utility-integration.md','symbol-win-flow.md'})
evidence={name:(ROOT/'Tools/Evidence'/name).read_text(encoding='utf-8-sig') for name in evidence_names}
inventory=[]
for p in sorted((ROOT/'Assets/Whitebox/Tests').glob('*.cs')):
    src=p.read_text(encoding='utf-8-sig')
    for m in re.finditer(r'public\s+(?:void|IEnumerator)\s+(\w+)\s*\(',src):
        prefix=src[max(0,m.start()-1800):m.start()]
        if re.search(r'\[(?:UnityTest|Test|TestCase)[^\]]*\]\s*(?:\[[^\]]*\]\s*)*$',prefix):
            inventory.append(dict(file=str(p.relative_to(ROOT)).replace('\\','/'),method=m.group(1)))
results=[]
for name in ['withdrawal-complete-tests.xml','task-native.xml','interstitial-verified.xml']:
    p=ROOT/'Artifacts'/name
    if not p.exists():continue
    node=ET.parse(p).getroot()
    results.append(dict(file=name,**{k:node.get(k,'') for k in ['result','total','passed','failed','start-time','end-time']},sha256=hashlib.sha256(p.read_bytes()).hexdigest()))

def image(path,label):
    p=Path(path)
    if not p.exists():return f'<div class="tech-card">截图未找到：{esc(label)}</div>'
    return '<figure class="phone"><img loading="lazy" src="data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()+'" alt="'+esc(label)+'"><span class="caption">'+esc(label)+'</span></figure>'
temp=Path('C:/Users/pc/AppData/Local/Temp')
original_main=image(temp/'codex-clipboard-a4ddafcc-675c-4ab2-9d93-facd51e6cdfe.png','用户提供的原版参考画面（含标注）')
original_tasks=image(temp/'codex-clipboard-3cf01c62-322d-45c9-a9e8-f0d8f6304f59.png','用户提供的原版每日任务')
original_cash=image(temp/'codex-clipboard-785fb5a4-aecb-4a54-ace9-d0672c3ed355.png','用户提供的原版提现任务局部')
port_cash=image(ROOT/'Artifacts/cash-withdrawal-tasks.png','复刻工程验证截图 · 不是原版实测')
reference=Path('C:/Users/pc/Downloads/step14_apk1_summary.html').read_text(encoding='utf-8-sig')
css=re.search(r'<style>(.*?)</style>',reference,re.S).group(1)
css=re.sub(r'data:image/[^;]+;base64,[A-Za-z0-9+/=]+','',css)
extra='''table{border-collapse:collapse;width:100%;min-width:680px;font-size:14px}th,td{text-align:left;padding:12px;border-bottom:1px solid var(--line);vertical-align:top}th{background:#fff3d6;position:sticky;top:0} .table-wrap{overflow:auto;max-height:650px;border:1px solid var(--line);border-radius:16px}input,select,button{font:inherit;padding:10px 12px;border:1px solid var(--line);border-radius:12px;background:white}button{cursor:pointer;color:#254d76} .filters{display:flex;gap:10px;flex-wrap:wrap;margin:16px 0}.filters input{flex:1;min-width:180px}.rules-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.rule{padding:20px;border-radius:18px;background:#fff;border:1px solid var(--line)}.rule p{margin:9px 0}.rule small{color:var(--muted)}pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:12px;line-height:1.6}.hero-art{padding:26px;display:grid;grid-template-columns:1fr 1fr;gap:12px;align-items:center}.hero-art figure{max-height:510px}.hero-art img{object-fit:cover;object-position:top}.status{white-space:nowrap;color:#916715}.two{display:grid;grid-template-columns:1fr 1fr;gap:22px}.covernote{background:#fff2d3;border-left:4px solid #e5b23a;padding:16px;border-radius:12px}.test-table{min-width:1080px}.test-table td:nth-child(6){min-width:250px}.test-table td:nth-child(7){min-width:280px}.empty{padding:30px;text-align:center}.section-no{color:#b47b20;font-size:13px;font-weight:900}.evidence-link{font-size:12px}.phone .caption{position:static;display:block;border-radius:0;margin:0}.phone img{max-height:600px;object-fit:contain!important;background:#f1e6d4;aspect-ratio:auto!important}.hero-art .phone img{height:440px;object-fit:cover!important;object-position:top}.test-result{padding:16px;background:#edf8ef;border-radius:16px}details summary{cursor:pointer}nav{position:sticky;top:0;background:#fff7e8eF;padding:12px;z-index:10;backdrop-filter:blur(10px)}@media(max-width:700px){nav{position:static}section[id]{scroll-margin-top:12px}.rules-grid,.two{grid-template-columns:1fr}.surface{padding:20px}.hero-art{padding:15px}.hero-art .phone img{height:300px}.hero-copy{padding:25px}.hero-stats{grid-template-columns:1fr 1fr}}@media print{nav,.filters{display:none}.table-wrap{max-height:none;overflow:visible}.rule{break-inside:avoid}}'''

parts=['<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Dragon Legend｜原版规则与测试用例</title><style>'+css+extra+'</style></head><body><main>']
parts.append(f'''<section class="surface hero"><div class="hero-grid"><div class="hero-copy"><div class="kicker">ORIGINAL GAME · RULES & TEST MATRIX</div><h1>Dragon Legend<br>原版规则与<br>测试用例</h1><p class="hero-line">按参考页的暖色图解样式，梳理<strong>从转动到提现审核</strong>的完整规则链。</p><div class="brand-meta">包名 com.sfflogdstudio.dragonlegend<br>主基线：包内 GoldenDragon_default / 非自然量 · 整理日期 2026-09-10</div><div class="verdict" style="margin-top:20px"><div class="verdict-num">{len(cases)}</div><div class="verdict-copy"><b>可执行测试用例</b><span>本轮为资料整理；按标注对象待执行，不虚标 PASS。</span></div></div><div class="hero-stats"><div class="hero-stat"><b>{len(rules)}</b><span>规则条目</span></div><div class="hero-stat"><b>5</b><span>提现档位</span></div><div class="hero-stat"><b>6+1</b><span>提现任务阶段</span></div><div class="hero-stat"><b>6</b><span>每日任务</span></div></div></div><div class="hero-art">{original_main}{original_tasks}</div></div></section>''')
parts.append('<nav>'+''.join(f'<a href="#{key}">{label}</a>' for key,label in [('scope','证据口径'),('overview','配置速览'),('flow','玩法与奖励'),('ads','广告节奏'),('daily','每日任务'),('cash','提现任务'),('rules','规则索引'),('tests','测试用例'),('evidence','证据与配置')])+'</nav>')
parts.append('''<section id="scope" class="surface"><div class="section-no">01 / 阅读口径</div><h2>原版规则、样本修改、复刻结果分开看</h2><div class="essentials"><div class="essential"><b>静态证据</b><span>来自原始 ARM64 方法、配置快照和原版资源。方法地址与原始字段保留在证据附录。</span></div><div class="essential"><b>原版画面</b><span>本页使用用户提供的原版截图作为视觉参考，不能据此证明所有分支已跑通。</span></div><div class="essential"><b>修改后的 MuMu 样本</b><span>#0 的库曾被修改以固定读取非自然量配置；其冷启动结果不能代表未修改包的线上分流。</span></div><div class="essential"><b>复刻工程回归</b><span>已有 Unity 测试验证复刻逻辑；本轮只读取记录，没有重跑原版、调用广告或提交提现。</span></div></div><p class="covernote" style="margin-top:18px">范围覆盖当前证据可支持的核心模块。服务器国家分流、真实广告供应商/no-fill 映射、后台收入、完整 RTP/收益衰减曲线及真实到账均未确认；不套用参考报告中另一款游戏的规则。涉及账户或付款的用例只在明确隔离的测试副本/测试接口执行。</p></section>''')

fields=[('初始原始现金','Qonrii','InirQoing'),('初始次数','Qonrii','InirGping'),('次数上限 / 累计门槛','Qonrii','MojGping'),('额外次数','Qonrii','OppGping'),('各级恢复秒数','Qonrii','GpinQP'),('插屏冷却秒数','Qonrii','InggrrQP'),('插屏概率阈值','Qonrii','InggrrRonpom'),('福袋计数门槛','Qonrii','RonkGpinQP'),('Big Win 阈值','Qonrii','Riikin'),('提现原始金额','Rgpggm','Qogt')]
rows=''.join(f'<tr><td>{label}</td><td class="mono">{section}.{key}</td><td>{esc(cfg[section][key])}</td><td>{esc(organic.get(section,{}).get(key,"未提取"))}</td></tr>' for label,section,key in fields)
parts.append('<section id="overview" class="surface"><div class="section-no">02 / 快照对照</div><h2>default 为主，organic 单独对照</h2><p class="subhead">数值直接取本地文件；只有明确标为美元的展示金额才除以 100。数组概率阈值不是百分比。</p><div class="table-wrap"><table><thead><tr><th>项目</th><th>字段</th><th>cp_test / default</th><th>包内 organic</th></tr></thead><tbody>'+rows+'</tbody></table></div></section>')
parts.append('''<section id="flow" class="surface"><div class="section-no">03 / 玩法链</div><h2>转动驱动奖励，特殊符号打开分支</h2><div class="story-flow">'''+''.join(f'<div class="story-step"><div class="story-icon">{icon}</div><b>{title}</b><span>{text}</span></div>' for icon,title,text in [('①','开始转动','校验忙碌状态、扣 1 次'),('②','结算奖励','按符号、bet 和奖励配置计算'),('③','Bonus / Free','集币与特殊符号进入对应流程'),('④','小游戏与宝藏','龙珠按颜色对应权重选择分支'),('⑤','进度回写','现金、次数、任务各自保存')])+'''</div><div class="explain-grid"><div class="explainer"><h3>Jackpot 金额</h3><p class="mono">raw = multiplier × bet + JpOpp × addCount<br>display = raw / 100</p><p>default：JpOpp=10。不能把浮点余额先按界面精度取整。</p></div><div class="explainer"><h3>Bonus 匹配</h3><p>Grand：招 · 财 · 进 · 宝<br>Major：宝 · 宝 · 宝<br>Minor：财 · 财 · 财</p><p class="micro-note">一张牌消费一个匹配位置；配牌使用同一配置行。</p></div></div><p class="covernote" style="margin-top:16px">目前没有足够证据给出完整收益衰减公式或 RTP。参考 HTML 中 Fruit Fusion 的 $500 衰减公式不适用于本游戏。</p></section>''')
parts.append('''<section id="ads" class="surface"><div class="section-no">04 / 广告</div><h2>有触发调用，不等于必定实播</h2><div class="story-flow">'''+''.join(f'<div class="story-step"><b>{x}</b><span>{y}</span></div>' for x,y in [('IsA / 等级','IsA 或最低等级拒绝'),('抽概率','0..999 ≤ 配置阈值'),('查冷却','成功时间 + 10 秒 ≤ now'),('SDK 请求','记录 placement / scene'),('成功回调','更新冷却和提现广告任务')])+'''</div><div class="two"><div class="explainer"><h3>主动奖励：额外次数</h3><p>extraspin / extraspin → 成功加 10；失败解锁重试；实际数量封顶 30。</p></div><div class="explainer"><h3>流程插屏：7 个调用点</h3><p>福袋、Big Win、每日任务奖励、Jackpot、网络错误、Lucky 奖励、宝藏。Free-start 普通关闭不在此列。</p></div></div><p class="micro-note">原版概率比较含等号：阈值 0 仍可能命中随机 0。冷却先后顺序、随机消耗和失败回调都是测试边界。</p></section>''')
tasks=cfg['Rogk'];daily=''.join(f'<tr><td>{i}</td><td>{esc(label)}</td><td>{tasks["RogkOmoinr"][n]}</td><td>${tasks["Rgkorp"][n]/100:g}</td><td>{tasks["Jimp"][n]}</td></tr>' for n,(i,label) in enumerate(zip(tasks['Ip'],['Big Win','Jackpot','触发 Free Game','Slots 小游戏（不是普通转动）','Bonus Game','登录'])))
parts.append('<section id="daily" class="surface"><div class="section-no">05 / 每日任务</div><h2>任务名称要对应真正的触发事件</h2><div class="table-wrap"><table><thead><tr><th>ID</th><th>事件</th><th>目标</th><th>基础奖励</th><th>Jump</th></tr></thead><tbody>'+daily+'</tbody></table></div><div class="essentials"><div class="essential"><b>重置时间</b><span>default 按本地日期/午夜；organic 不直接套用同样重置。</span></div><div class="essential"><b>领取顺序</b><span>先保存领取标记，随后奖励表现与到账；跳转分支决定奖励倍率。</span></div></div></section>')
labels=['Spin','Watch ads','Claim Jackpots','Claim BigWins','Claim Treasures','Play FreeGames']
cashrows=''.join('<tr><td>'+esc(label)+'</td>'+''.join('<td>'+str(v)+'</td>' for v in cfg['Rgpggm'][f'Rogk{n+1}roil'])+'<td>24 小时</td></tr>' for n,label in enumerate(labels))
parts.append('<section id="cash" class="surface"><div class="section-no">06 / 提现任务</div><h2>现金门槛之后，逐阶段检查次数与等待</h2><p class="subhead">下表为 default 的 fail / review 任务分支；不是所有服务器结果的统一承诺。</p><div class="table-wrap"><table><thead><tr><th>阶段</th>'+''.join(f'<th>${v/100:,.0f}</th>' for v in cfg['Rgpggm']['Qogt'])+'<th>每阶段等待</th></tr></thead><tbody>'+cashrows+'</tbody></table></div><div class="ending"><strong>第 6 号阶段：收集宝藏</strong><span>完成前述阶段后仍有配置收集条件；不保证付款，不等于固定七天到账。</span></div><div class="two" style="margin-top:20px">'+original_cash+port_cash+'</div></section>')
parts.append('<section id="rules" class="surface"><div class="section-no">07 / 规则索引</div><h2>每项结论保留来源</h2><div class="rules-grid">'+''.join(f'<article class="rule" id="{r["id"]}"><span class="badge">{r["id"]} · {r["module"]}</span><h3 style="margin-top:10px">{r["title"]}</h3><p>{r["body"]}</p><a class="evidence-link" href="#ev-{evidence_names.index(r["evidence"])}">证据：{r["evidence"]}</a></article>' for r in rules)+'</div></section>')
parts.append(f'''<section id="tests" class="surface"><div class="section-no">08 / 测试矩阵</div><h2>{len(cases)} 个用例，包含前置条件、步骤和预期</h2><p class="subhead">所有用例均待执行，修改样本与复刻兼容用例另行标注。可控 RNG、时钟、牌组的用例需要白盒测试入口；不具备条件时记为阻塞，不能改为 PASS。</p><div class="filters"><input id="search" aria-label="搜索用例" placeholder="搜索：广告、24 小时、TC001…"><select id="module" aria-label="模块"><option value="">全部模块</option>'''+''.join(f'<option>{m}</option>' for m in dict.fromkeys(r['module'] for r in rules))+'''</select><button id="csv">下载用例 CSV</button><button onclick="window.print()">打印 / 保存 PDF</button></div><p class="small" id="count"></p><div class="table-wrap"><table class="test-table"><thead><tr><th>ID / 规则</th><th>模块</th><th>优先级</th><th>状态</th><th>前置条件</th><th>操作步骤</th><th>预期结果</th></tr></thead><tbody id="cases"></tbody></table></div></section>''')
parts.append('<section class="surface"><div class="section-no">09 / 已有复刻测试记录</div><h2>读取历史结果，不把复刻 PASS 算作原版 PASS</h2><div class="tech-grid">'+''.join(f'<div class="test-result"><h3>{esc(r["file"])}</h3><b>{esc(r["passed"])} / {esc(r["total"])} 通过</b><p class="small">记录状态 {esc(r["result"])}<br>{esc(r["start-time"])}<br>复刻 Unity PlayMode，非本轮新跑</p></div>' for r in results)+f'</div><details class="tech"><summary>测试源码方法清单 · {len(inventory)} 个候选方法</summary><p class="small">按源码 Test / UnityTest / TestCase 标记提取；参数化用例展开数与方法数不同，不能把方法数当作覆盖率。</p><input id="inv-search" placeholder="过滤测试文件 / 方法" style="width:100%"><div class="table-wrap"><table><thead><tr><th>测试文件</th><th>方法</th></tr></thead><tbody id="inventory"></tbody></table></div></details></section>')
parts.append('<section id="evidence" class="surface"><div class="section-no">10 / 可追溯附录</div><h2>原始配置与审核笔记</h2><p class="subhead">笔记包含历史“尚未实现”的状态，不能当作当前工程状态；只提取标明地址的原版事实。最新工程状态应结合测试 XML 与源代码核对。</p>')
for i,(name,content) in enumerate(evidence.items()):
    parts.append(f'<details id="ev-{i}" class="tech"><summary>{esc(name)}</summary><div class="tech-body"><p class="mono">Tools/Evidence/{esc(name)}</p><pre>{esc(content)}</pre></div></details>')
parts.append('<details class="tech"><summary>default 全量配置 · 7 个原始字段分组</summary><div class="tech-body"><pre>'+esc(json.dumps(cfg,ensure_ascii=False,indent=2))+'</pre></div></details>')
sources=[]
for p in [ROOT/'Assets/StreamingAssets/RecoveredConfig/Remote/cp_test.json',AUDIT/'config/GoldenDragon_default.json',AUDIT/'config/GoldenDragon_organic.json']:
    sources.append({'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
parts.append('<details class="tech"><summary>配置文件 SHA-256 与版本口径</summary><div class="tech-body"><pre>'+esc(json.dumps(sources,ensure_ascii=False,indent=2))+'</pre><p>包内提取目录为 2026-09-07 样本；本报告不自动适用于新 APK 或之后下发的远程配置。</p></div></details></section>')
payload={'rules':rules,'cases':cases,'inventory':inventory,'historical_results':results,'sources':sources}
parts.append('<footer>Dragon Legend · 2026-09-10 · 单文件离线 HTML · 本轮只做规则与用例整理，未修改游戏、未执行原版用例</footer></main><script id="data" type="application/json">'+json.dumps(payload,ensure_ascii=False).replace('<','\\u003c')+'</script>')
parts.append('''<script>const D=JSON.parse(document.getElementById('data').textContent), E=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));function render(){let q=document.getElementById('search').value.toLowerCase(),m=document.getElementById('module').value;let rows=D.cases.filter(r=>(!m||r.module===m)&&Object.values(r).join(' ').toLowerCase().includes(q));document.getElementById('count').textContent=`显示 ${rows.length} / ${D.cases.length} 项 · 原版本轮执行 0 项`;document.getElementById('cases').innerHTML=rows.map(r=>`<tr><td>${r.id}<br><a href="#${r.rule}">${r.rule}</a></td><td>${E(r.module)}</td><td>${r.priority}</td><td class="status">${r.status}</td><td>${E(r.precondition)}</td><td>${E(r.steps)}</td><td>${E(r.expected)}</td></tr>`).join('')||'<tr><td colspan="7" class="empty">没有匹配的用例</td></tr>'}function inv(){let q=document.getElementById('inv-search').value.toLowerCase();document.getElementById('inventory').innerHTML=D.inventory.filter(r=>(r.file+' '+r.method).toLowerCase().includes(q)).map(r=>`<tr><td class="mono">${E(r.file)}</td><td>${E(r.method)}</td></tr>`).join('')}document.getElementById('search').oninput=render;document.getElementById('module').onchange=render;document.getElementById('inv-search').oninput=inv;document.getElementById('csv').onclick=()=>{let keys=['id','rule','module','priority','precondition','steps','expected','status'],quote=x=>'"'+String(x).replaceAll('"','""')+'"';let s='\\ufeff'+[keys.join(','),...D.cases.map(r=>keys.map(k=>quote(r[k])).join(','))].join('\\r\\n');let u=URL.createObjectURL(new Blob([s],{type:'text/csv;charset=utf-8'})),a=document.createElement('a');a.href=u;a.download='dragonlegend_original_test_cases.csv';a.click();setTimeout(()=>URL.revokeObjectURL(u),1000)};render();inv();</script></body></html>''')
dest=OUT/'dragonlegend_original_rules_cases.html'
dest.write_text(''.join(parts),encoding='utf-8')
keys=['id','rule','module','priority','precondition','steps','expected','status']
with (OUT/'dragonlegend_original_test_cases.csv').open('w',newline='',encoding='utf-8-sig') as f:
    w=csv.DictWriter(f,fieldnames=keys);w.writeheader();w.writerows(cases)
(OUT/'dragonlegend_original_rules_data.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'html':str(dest),'rules':len(rules),'cases':len(cases),'test_methods':len(inventory),'historical_results':results,'size':dest.stat().st_size},ensure_ascii=False))
