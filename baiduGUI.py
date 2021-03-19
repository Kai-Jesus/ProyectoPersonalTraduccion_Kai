import requests
import  tkinter as tk
import execjs

def crak():
    query = t1.get(0.0,"end")
    with open('baidu_translata_js.js', 'r', encoding='utf-8') as f:
        ctx = execjs.compile(f.read())

    sign = ctx.call('e', query)
    # print(sign)
    fanyi(sign)

def fanyi(sign):
    url = "https://fanyi.baidu.com/v2transapi?"
    data = {
        "from": "en",
        "to": "zh",
        "query": t1.get(0.0,"end"),
        "transtype": "realtime",
        "simple_means_flag": "3",
        "sign": sign,
        "token": "31955601ac64be0d35b70735271091ba",
    }
    header = {
    "Cookie": "BAIDUID=14C13828C12434EA0452845C4DB0F2CC:FG=1; BAIDUID_BFESS=2E3E3C34D77E43C7B7B27A583E37877C:FG=1; BIDUPSID=14C13828C12434EA0452845C4DB0F2CC; PSTM=1612027652; delPer=0; BDRCVFR[feWj1Vr5u3D]=I67x6TjHwwYf0; __yjs_duid=1_c4c86c7e6d4366f7626e4f40e21306221612980569093; BDRCVFR[FhauBQh29_R]=mbxnW11j9Dfmh7GuZR8mvqV; H_WISE_SIDS=107316_110085_127969_128698_131423_132549_133333_144966_154213_156288_156927_162186_163568_164075_164455_165135_165737_166147_167069_167085_167114_167296_168033_168205_168310_168542_168558_168632_168763_169156_169216_169308_169374_169658_169789_170251; PSINO=7; ZD_ENTRY=google; BDUSS=FJOUDR4UXE4cnZUVlNRc25pdzFqYWNzNHZ-UVpaaEpiQjA2Q3Rrb2VLckJBV1pnRVFBQUFBJCQAAAAAAAAAAAEAAADkxneEU2h1dGdoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMF0PmDBdD5gM; BDUSS_BFESS=FJOUDR4UXE4cnZUVlNRc25pdzFqYWNzNHZ-UVpaaEpiQjA2Q3Rrb2VLckJBV1pnRVFBQUFBJCQAAAAAAAAAAAEAAADkxneEU2h1dGdoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMF0PmDBdD5gM; REALTIME_TRANS_SWITCH=1; FANYI_WORD_SWITCH=1; HISTORY_SWITCH=1; SOUND_SPD_SWITCH=1; SOUND_PREFER_SWITCH=1; H_PS_PSSID=33344_31254_33689_33594_33570_33392_33622_33460_33268; BDORZ=B490B5EBF6F3CD402E515D22BCDA1598; Hm_lvt_64ecd82404c51e03dc91cb9e8c025574=1615314967,1615400217; Hm_lpvt_64ecd82404c51e03dc91cb9e8c025574=1615400538; ab_sr=1.0.0_ZmJhOTllMDM5ZmE5N2FmYzE3OWQyNTQyYWFhY2Y4Yjk4NDYzZTJhMzQxODliNWRjMWNkMmI3ZmNmNGU5MDMyYjViZmViYzYzMWUxYTQwZmFmOWUzOTBlODEyYTY5MTM2; __yjsv5_shitong=1.0_7_5808c3274c21317936003722d248af900d03_300_1615400540020_83.58.12.153_29c6bb37",
    "User - Agent": "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 87.0 .4280 .141 Safari / 537.36"
    }
    # print(t1.get(0.0,"end"))
    response = requests.post(url,data=data, headers=header).json()
    print(response)
    # print(response['trans_result']['data'])
    r = response['trans_result']['data'][2]['dst']
    # t1.delete(0.0, "end")
    for item in response['trans_result']['data']:
        # print(item['dst'])
        i = item['dst'] + '\n' + '\n'
        t2.insert("end",i)
# fanyi(query)
root = tk.Tk()
root.title("翻译软件")
root.geometry("800x400")
l1 = tk.Label(root,text="请输入翻译内容：")
l1.grid()
t1 = tk.Text(root,width=56,height=20)
t1.grid()
t2 = tk.Text(root,width=56,height=20)
t2.grid(row=1,column=1)
b1 = tk.Button(root,text="中文翻译",width=8,command = crak)
b1.grid(row=2,column=0)
b2 = tk.Button(root,text="英文翻译",width=8)
b2.grid(row=3,column=0)

def cp():
    t1.delete(0.0, "end")

b3 = tk.Button(root,text="清零",width=8,command = cp)
b3.grid(row=4,column=0)
root.mainloop()


if __name__ == "__main__":
    while True:
        crak()