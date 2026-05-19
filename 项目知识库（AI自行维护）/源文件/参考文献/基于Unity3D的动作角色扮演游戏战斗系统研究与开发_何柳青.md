Traceback (most recent call last):
  File "<frozen runpy>", line 198, in _run_module_as_main
  File "<frozen runpy>", line 88, in _run_code
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\Scripts\markitdown.exe\__main__.py", line 6, in <module>
    sys.exit(main())
             ~~~~^^
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\markitdown\__main__.py", line 93, in main
    _handle_output(args, result)
    ~~~~~~~~~~~~~~^^^^^^^^^^^^^^
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\markitdown\__main__.py", line 102, in _handle_output
    print(result.text_content)
    ~~~~~^^^^^^^^^^^^^^^^^^^^^
UnicodeEncodeError: 'gbk' codec can't encode character '\ufb00' in position 1311: illegal multibyte sequence
