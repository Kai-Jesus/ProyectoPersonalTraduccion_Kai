from tkinter import *

win = Tk()
win.title("KaiTranstlate")

btn = Button(text="btn")
btn.config(bg="skyblue")
btn.config()
btn.pack()

win.geometry("400x200")
win.minsize(width=400, height=200)
# win.maxsize(width=400, height=200)
# win.resizable(False,False)

win.iconbitmap("")

# win.config(bg="skyblue")

win.attributes("-alpha",0.725)

win.attributes("-topmost",1)

win.mainloop()

