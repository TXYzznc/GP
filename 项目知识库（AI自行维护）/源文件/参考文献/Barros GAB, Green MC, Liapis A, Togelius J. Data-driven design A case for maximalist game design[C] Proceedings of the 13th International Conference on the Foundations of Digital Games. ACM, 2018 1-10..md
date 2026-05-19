Traceback (most recent call last):
  File "<frozen runpy>", line 198, in _run_module_as_main
  File "<frozen runpy>", line 88, in _run_code
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\Scripts\markitdown.exe\__main__.py", line 6, in <module>
    sys.exit(main())
             ~~~~^^
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\markitdown\__main__.py", line 91, in main
    result = markitdown.convert(args.filename)
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\markitdown\_markitdown.py", line 1563, in convert
    return self.convert_local(source, **kwargs)
           ~~~~~~~~~~~~~~~~~~^^^^^^^^^^^^^^^^^^
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\markitdown\_markitdown.py", line 1583, in convert_local
    for g in self._guess_ext_magic(path):
             ~~~~~~~~~~~~~~~~~~~~~^^^^^^
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\markitdown\_markitdown.py", line 1756, in _guess_ext_magic
    guesses = puremagic.magic_file(path)
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\puremagic\main.py", line 343, in magic_file
    head, foot = file_details(filename)
                 ~~~~~~~~~~~~^^^^^^^^^^
  File "C:\Users\WIN10\AppData\Roaming\Python\Python314\site-packages\puremagic\main.py", line 237, in file_details
    raise PureError("Not a regular file")
puremagic.main.PureError: Not a regular file
