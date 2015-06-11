using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BrainFrack
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }


        /*
        From Wikipedia BrainFuck page - http://en.wikipedia.org/wiki/Brainfuck

        > increment the data pointer (to point to the next cell to the right). 
        < decrement the data pointer (to point to the next cell to the left). 
        + increment (increase by one) the byte at the data pointer. 
        - decrement (decrease by one) the byte at the data pointer. 
        . output the byte at the data pointer. 
        , accept one byte of input, storing its value in the byte at the data pointer. 
        [ if the byte at the data pointer is zero, then instead of moving the instruction pointer forward to the next command, jump it forward to the command after the matching ] command. 
        ] if the byte at the data pointer is nonzero, then instead of moving the instruction pointer forward to the next command, jump it back to the command after the matching [ command. 

        // Hello World
        0         0         0         0         0         0         0         0         0         0         1
        0         1         2         3         4         5         6         7         8         9         0  
        01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
        ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++.

        */

        string bfCode = string.Empty;
        List<int> bfMemory = new List<int>();
        List<Loop> bfLoops = new List<Loop>();
        List<Loop> bfStack = new List<Loop>();

        int iCellPtr = 0;
        int iCodeLen = 0;
        int iStackPtr = -1;  // not in any loops.

        private void RunProgram_Click(object sender, RoutedEventArgs e)
        {
            iCodeLen = BFListing.Text.Length;
            if (iCodeLen > 0)
            {
                bfCode = BFListing.Text;
                int iCount = BuildLoopArray(bfCode);  // get count of loops or -1 if error.
                if (-1 != iCount)   // if != -1 then we either have no loops or correctly matched brackets on loops.
                {
                    int iInstrPtr = 0;
                    bool bRunning = true;
                    bfMemory.Add(0);    // first memory cell.

                    while (bRunning)
                    {
                        char cInstr = bfCode[iInstrPtr];
                        switch (cInstr)
                        {
                            case '>':   // move to next data cell to the right
                                Debug.WriteLine(string.Format("Add new cell (count {0})", bfMemory.Count));
                                iCellPtr++;
                                if (iCellPtr > bfMemory.Count - 1)
                                    bfMemory.Add(0);
                                Debug.WriteLine(string.Format("Move Cell Right (count {0})", bfMemory.Count));
                                break;
                            case '<':
                                if (iCellPtr > 0)
                                    iCellPtr--;
                                Debug.WriteLine(string.Format("Move Cell Left, now {0} (count {1})", iCellPtr,bfMemory.Count));
                                break;
                            case '+':
                                int iValue = bfMemory[iCellPtr];
                                iValue++;
                                bfMemory[iCellPtr] = iValue;
                                Debug.WriteLine(string.Format("Increment Cell #{0} - New Value {1}", iCellPtr, bfMemory[iCellPtr]));
                                break;
                            case '-':
                                if (bfMemory[iCellPtr] > 0)
                                {
                                    iValue = bfMemory[iCellPtr];
                                    iValue--;
                                    bfMemory[iCellPtr] = iValue;
                                    Debug.WriteLine(string.Format("Decrement Cell #{0} - New Value {1}", iCellPtr, bfMemory[iCellPtr]));
                                }
                                else
                                {
                                    // what's the behavior when a memory cell is decremented below zero?
                                }
                                break;
                            case '.':
                                Debug.WriteLine(string.Format("Output Character \"{0}\"", bfMemory[iCellPtr]));
                                BFOutput.Text += (char)bfMemory[iCellPtr];
                                break;
                            case ',':
                                Debug.WriteLine("//TODO: input character here");
                                // TODO: accept one byte of input.
                                break;
                            case '[':
                                Debug.WriteLine("Start of loop");
                                Loop l = GetLoopFromStartPosition(iInstrPtr);
                                if (bfStack.Count > 0)  // nothing on the stack. add the loop.
                                {
                                    if (bfStack[bfStack.Count-1].iStart != iInstrPtr)
                                        bfStack.Add(l);
                                }
                                else
                                {
                                    // nothing in the stack, push our loop onto the stack.
                                    bfStack.Add(l);
                                }

                                if (bfMemory[iCellPtr] == 0)    // if current data pointer is [zero].
                                {
                                    Debug.WriteLine("Current Cell is Zero - pop stack and jump to end of loop");
                                    // Pop the current Loop off the stack.
                                    if (bfStack.Count > -1)
                                    {
                                        iInstrPtr = bfStack[bfStack.Count - 1].iEnd;
                                        bfStack.RemoveAt(bfStack.Count - 1);    // remove the last item.
                                    }
                                    Debug.WriteLine(string.Format("New InstrPtr {0}", iInstrPtr));
                                }
                                break;
                            case ']':
                                if (bfMemory[iCellPtr] != 0)    // current data pointer is non-zero.
                                {
                                    Debug.WriteLine("End of loop. Cell contents != 0, jump to start of loop");
                                    if (bfStack.Count > 0)
                                        iInstrPtr = bfStack[bfStack.Count-1].iStart;
                                    else
                                    {
                                        // whoops, error...
                                        bRunning = false;
                                    }
                                }
                                else
                                {
                                    if (bfStack.Count > -1)
                                    {
                                        iInstrPtr = bfStack[bfStack.Count - 1].iEnd;
                                        bfStack.RemoveAt(bfStack.Count - 1);    // remove the last item.
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                        iInstrPtr++;
                        Debug.WriteLine("InstrPtr : " + iInstrPtr.ToString() + " of " + iCodeLen.ToString() + "");

                        Debug.WriteLine("--------------------------------------");
                        Debug.WriteLine(string.Format("Instruction {0}, bfMemory {1}", iInstrPtr, iCellPtr));
                        for(int x=0;x < bfMemory.Count;x++)
                        {
                            Debug.WriteLine(string.Format("[{0}] = {1}", x, bfMemory[x]));
                        }

                        if (iInstrPtr >= iCodeLen)
                            bRunning = false;
                    }
                    // run code.
                }
                else
                {
                    // show message dialog.
                }
            }
            else
            {
                // message... no code == no output :)
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BFListing.Text = "++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++.";
        }

        Loop GetLoopFromStartPosition(int iStartPos)
        {
            foreach (Loop l in bfLoops)
            {
                if (l.iStart == iStartPos)
                    return l;
            }
            return null;
        }

        int BuildLoopArray(string strCode)
        {
            // return types...
            // 0 == no loops (which is ok)
            // > 0 == number of loops (which is ok)
            // -1 == mismatched loop braces [not ok - failure].
            bool bFail = false;
            int iRet = 0;

            for(int x=0;x < strCode.Length;x++)
            {
                char c = strCode[x];
                int iEnd = -1;
                if (c == '[')
                {
                    int inCount = 1;
                    for(int y=x+1;y < strCode.Length;y++)
                    {
                        char n = strCode[y];
                        if ('[' == n)
                        {
                            inCount++;
                        }
                        if (']' == n)
                        {
                            inCount--;
                            if (inCount == 0)
                            {
                                iEnd = y;
                                break;
                            }
                        }
                    }

                    if (iEnd != -1)
                        bfLoops.Add(new Loop(x, iEnd));
                    else
                    {
                        bFail = true;
                    }
                }
            }

            Debug.WriteLine("Loop Stack\n");
            if (bfLoops.Count == 0)
                Debug.WriteLine("No loops detected...");
            else
            {
                foreach (Loop l in bfLoops)
                {
                    Debug.WriteLine(string.Format("Start {0} End {1}", l.iStart, l.iEnd));
                }
            }

            if (bFail)
                return -1;

        return bfLoops.Count;
        }

    }

    #region Class Loop
    class Loop
    {
        public Loop(int start, int end)
        {
            _iStart = start;
            _iEnd = end;
        }

        public int iStart
        {
            get
            {
                return this._iStart;
            }

            set
            {
                this._iStart = value;
            }
        }

        public int iEnd
        {
            get
            {
                return this._iEnd;
            }

            set
            {
                this._iEnd = value;
            }
        }

        private int _iStart;
        private int _iEnd;
    }
#endregion
}
