"""Windows Media Foundation video source. All COM calls and frame ownership stay on the worker thread."""

from contextlib import ExitStack, contextmanager, nullcontext
import ctypes as c
import hashlib
import time
import uuid

from frame_pipeline import Frame

PTR = c.c_void_p
U32 = c.c_uint32
I32 = c.c_int32
U64 = c.c_uint64
I64 = c.c_int64
OUT = c.POINTER(PTR)
VIDEO_STREAM = 0xFFFFFFFC
ALL_STREAMS = 0xFFFFFFFE
NO_MORE_TYPES = 0xC00D36B9
INVALID_MEDIA_TYPE = 0xC00D36B4
MAX_DEVICES = 64
MAX_PIXELS = 1920 * 1080


def guid(value):
    return (c.c_ubyte * 16).from_buffer_copy(uuid.UUID(value).bytes_le)


# Windows SDK mfapi.h, mfidl.h, mfobjects.h and mfreadwrite.h.
SOURCE_TYPE = guid("c60ac5fe-252a-478f-a0ef-bc8fa5f7cad3")
VIDEO_DEVICE = guid("8ac3587a-4ae7-42d8-99e0-0a6013eef90f")
FRIENDLY_NAME = guid("60d0e559-52f8-4fa2-bbce-acdb34a8ec01")
SYMBOLIC_LINK = guid("58f0aad8-22bf-4f8a-bb3d-d2c4978c6e2f")
MAX_VIDEO_BUFFERS = guid("7dd9b730-4f2d-41d5-8f95-0cc9a912ba26")
MAJOR_TYPE = guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f")
SUBTYPE = guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5")
VIDEO = guid("73646976-0000-0010-8000-00aa00389b71")
RGB32 = guid("00000016-0000-0010-8000-00aa00389b71")
FRAME_SIZE = guid("1652c33d-d6b2-4012-b834-72030849a37d")
FRAME_RATE = guid("c459a2e8-3d2c-4e44-b132-fee5156c7bb0")
DEFAULT_STRIDE = guid("644b4e48-1e02-4516-b0eb-c01ca9d49ac6")
VIDEO_PROCESSING = guid("fb394f3d-ccf1-42ee-bbb3-f9b845d5681d")
LOW_LATENCY = guid("9c27891a-ed7a-40e1-88e8-b22727a024ee")
DISCONNECT_ON_SHUTDOWN = guid("56b67165-219e-456d-a22e-2d3004c7fe56")
BUFFER_2D = guid("33ae5ea6-4316-436f-8ddd-d73d22f829ec")


class CameraError(Exception):
    def __init__(self, code, native_code=None):
        super().__init__(code)
        self.code = code
        self.native_code = native_code


def check(hr, fallback="runtime-failure"):
    if hr >= 0:
        return
    value = hr & 0xFFFFFFFF
    if value == 0x80070005:  # E_ACCESSDENIED
        raise CameraError("permission-denied", value)
    if value in (0x80070020, 0x80070021, 0x800700AA, 0x80070964, 0xC00D3EA3):
        raise CameraError("device-busy", value)
    if value in (0x8007048F, 0x80070651, 0xC00D3EA2):
        raise CameraError("device-lost", value)
    if value == INVALID_MEDIA_TYPE:
        raise CameraError("unsupported-format", value)
    raise CameraError(fallback, value)


def export(dll, name, *types, result=I32):
    function = getattr(dll, name)
    function.argtypes, function.restype = types, result
    return function


def call(obj, slot, types, *args, result=I32):
    # COM vtable slots are from the Windows SDK C ABI, including inherited methods.
    vtable = c.cast(obj, c.POINTER(c.POINTER(PTR)))[0]
    return c.WINFUNCTYPE(result, PTR, *types)(vtable[slot])(obj, *args)


def release(obj):
    if obj:
        call(obj, 2, (), result=U32)  # IUnknown::Release


def set_guid(obj, key, value):
    check(call(obj, 24, (PTR, PTR), key, value))  # IMFAttributes::SetGUID


def set_u32(obj, key, value):
    check(call(obj, 21, (PTR, U32), key, value))


def get_number(obj, key, kind, slot):
    value = kind()
    hr = call(obj, slot, (PTR, c.POINTER(kind)), key, c.byref(value))
    return value.value if hr >= 0 else None


@contextmanager
def multithreaded_com():
    ole = c.WinDLL("ole32")
    check(export(ole, "CoInitializeEx", PTR, U32)(None, 0))  # MTA
    try:
        yield
    finally:
        export(ole, "CoUninitialize", result=None)()


@contextmanager
def foundation():
    platform = c.WinDLL("mfplat")
    with multithreaded_com():
        check(export(platform, "MFStartup", U32, U32)(0x00020070, 0))
        try:
            yield platform
        finally:
            export(platform, "MFShutdown")()


def attributes(stack, platform):
    obj = PTR()
    check(export(platform, "MFCreateAttributes", OUT, U32)(c.byref(obj), 4))
    stack.callback(release, obj)
    return obj


def device_attributes(stack, platform):
    obj = attributes(stack, platform)
    set_guid(obj, SOURCE_TYPE, VIDEO_DEVICE)
    return obj


def get_string(obj, key, free, limit):
    pointer, length = PTR(), U32()
    try:
        hr = call(obj, 13, (PTR, OUT, c.POINTER(U32)), key, c.byref(pointer), c.byref(length))
        if hr >= 0 and pointer and 0 < length.value <= limit:
            return c.wstring_at(pointer, length.value)
        return ""
    finally:
        free(pointer)


def devices(platform):
    """Read only camera metadata; do not activate any of the enumerated sources."""
    free = export(c.WinDLL("ole32"), "CoTaskMemFree", PTR, result=None)
    mf = c.WinDLL("mf")
    array, count = c.POINTER(PTR)(), U32()
    with ExitStack() as stack:
        attrs = device_attributes(stack, platform)
        check(export(mf, "MFEnumDeviceSources", PTR, c.POINTER(c.POINTER(PTR)), c.POINTER(U32))(
            attrs, c.byref(array), c.byref(count)))
        try:
            found = {}
            for i in range(min(count.value, MAX_DEVICES)):
                link = get_string(array[i], SYMBOLIC_LINK, free, 4096)
                if not link:
                    continue
                name = get_string(array[i], FRIENDLY_NAME, free, 256) or "Windows camera"
                key = hashlib.sha256(link.casefold().encode("utf-8", errors="surrogatepass")).hexdigest()
                found.setdefault(key, (name, link))
            return found
        finally:
            for i in range(count.value):
                release(array[i])
            free(array)


def list_cameras():
    with foundation() as platform:
        return [{"id": key, "name": name} for key, (name, _) in devices(platform).items()]


def video_layout(reader, platform):
    media = PTR()
    check(call(reader, 6, (U32, OUT), VIDEO_STREAM, c.byref(media)), "unsupported-format")
    try:
        subtype = (c.c_ubyte * 16)()
        check(call(media, 10, (PTR, PTR), SUBTYPE, subtype), "unsupported-format")
        size = get_number(media, FRAME_SIZE, U64, 8) or 0
        width, height = size >> 32, size & 0xFFFFFFFF
        if bytes(subtype) != bytes(RGB32) or not valid_size(width, height):
            raise CameraError("unsupported-format")
        value = get_number(media, DEFAULT_STRIDE, U32, 7)
        if value is None:
            stride = I32()
            check(export(platform, "MFGetStrideForBitmapInfoHeader", U32, U32, c.POINTER(I32))(
                22, width, c.byref(stride)), "unsupported-format")
            value = stride.value
        return width, height, I32(value).value
    finally:
        release(media)


def valid_size(width, height):
    return 0 < width <= 1920 and 0 < height <= 1920 and width * height <= MAX_PIXELS


def configure(reader, platform, diagnostic=None, live=False):
    check(call(reader, 4, (U32, I32), ALL_STREAMS, 0))
    check(call(reader, 4, (U32, I32), VIDEO_STREAM, 1), "unsupported-format")
    candidates = []
    offered = []
    for i in range(256):
        native = PTR()
        hr = call(reader, 5, (U32, U32, OUT), VIDEO_STREAM, i, c.byref(native))
        if hr & 0xFFFFFFFF == NO_MORE_TYPES:
            break
        check(hr, "unsupported-format")
        try:
            size = get_number(native, FRAME_SIZE, U64, 8) or 0
            width, height = size >> 32, size & 0xFFFFFFFF
            rate = get_number(native, FRAME_RATE, U64, 8) or 0
            fps = (rate >> 32) / max(1, rate & 0xFFFFFFFF)
            subtype = (c.c_ubyte * 16)()
            check(call(native, 10, (PTR, PTR), SUBTYPE, subtype), "unsupported-format")
            kind = uuid.UUID(bytes_le=bytes(subtype))
            offered.append({"width": width, "height": height, "fps": fps, "subtype": str(kind)})
            color = kind.time_low in (20, 21, 22, 0x3231564E, 0x32595559, 0x47504A4D, 0x34363248,
                                     0x59565955, 0x32315659, 0x30323449)
            if valid_size(width, height) and (color or not live):
                candidates.append((abs(width - 640) + abs(height - 480), abs(fps - 30), i))
        finally:
            release(native)
    if diagnostic:
        diagnostic.data["offeredFormats"] = offered
    output = PTR()
    check(export(platform, "MFCreateMediaType", OUT)(c.byref(output)))
    try:
        set_guid(output, MAJOR_TYPE, VIDEO)
        set_guid(output, SUBTYPE, RGB32)
        for _, _, i in sorted(candidates):
            native = PTR()
            check(call(reader, 5, (U32, U32, OUT), VIDEO_STREAM, i, c.byref(native)), "unsupported-format")
            try:
                check(call(reader, 7, (U32, PTR, PTR), VIDEO_STREAM, None, native), "unsupported-format")
                check(call(reader, 7, (U32, PTR, PTR), VIDEO_STREAM, None, output), "unsupported-format")
                layout = video_layout(reader, platform)
                if diagnostic:
                    diagnostic.data["selectedNativeFormat"] = offered[i]
                    diagnostic.data["outputFormat"] = {"width": layout[0], "height": layout[1],
                                                        "stride": layout[2], "subtype": "RGB32"}
                return layout
            except CameraError as error:
                if error.code != "unsupported-format":
                    raise
            finally:
                release(native)
        raise CameraError("unsupported-format")
    finally:
        release(output)


def copy_bgr(np, width, height, start, length, first, stride):
    """Validate the entire address range before copying from a locked Windows buffer."""
    if not start or not first or not valid_size(width, height):
        raise CameraError("unsupported-format")
    offset = first - start
    last = offset + stride * (height - 1)
    low, high = min(offset, last), max(offset, last) + width * 4
    if abs(stride) < width * 4 or low < 0 or high > length or high - low > 16 * 1024 * 1024:
        raise CameraError("unsupported-format")
    data = c.string_at(start + low, high - low)
    return np.ndarray((height, width, 3), np.uint8, data, offset - low, (stride, 4, 1))


def sample_pixels(sample, np, width, height, default_stride):
    buffer, two_d = PTR(), PTR()
    check(call(sample, 41, (OUT,), c.byref(buffer)))  # IMFSample::ConvertToContiguousBuffer
    try:
        hr = call(buffer, 0, (PTR, OUT), BUFFER_2D, c.byref(two_d))
        start, first, length, stride = PTR(), PTR(), U32(), I32()
        if hr >= 0:
            check(call(two_d, 10, (U32, OUT, c.POINTER(I32), OUT, c.POINTER(U32)),
                       1, c.byref(first), c.byref(stride), c.byref(start), c.byref(length)))
            try:
                return copy_bgr(np, width, height, start.value, length.value, first.value, stride.value)
            finally:
                call(two_d, 4, ())  # IMF2DBuffer::Unlock2D
        check(call(buffer, 3, (OUT, PTR, c.POINTER(U32)), c.byref(start), None, c.byref(length)))
        try:
            origin = (start.value or 0) + max(0, -default_stride * (height - 1))
            return copy_bgr(np, width, height, start.value, length.value, origin, default_stride)
        finally:
            call(buffer, 4, ())  # IMFMediaBuffer::Unlock
    finally:
        release(two_d)
        release(buffer)


class CameraFrameSource:
    def __init__(self, reader, layout, np, file_source, platform, diagnostic=None):
        self.reader, self.layout, self.np, self.file_source = reader, layout, np, file_source
        self.platform = platform
        self.last_pts = None
        self.offset_ns = None
        self.samples_read = 0
        self.skipped = 0
        self.next_frame = 0
        self.diagnostic = diagnostic
        self.converted = 0
        self.last_frame_ms = None
        self.last_sample_at = time.monotonic()
        self.flags = {}

    def read(self, stop):
        if self.file_source:
            stop.wait(max(0.0, self.next_frame - time.perf_counter()))
        while not stop.is_set():
            if not self.file_source and time.monotonic() - self.last_sample_at >= 2.5:
                raise CameraError("stream-interrupted" if self.converted else "no-frames")
            flags, pts, sample = U32(), I64(), PTR()
            read_at = time.perf_counter_ns()
            hr = call(self.reader, 9, (U32, U32, PTR, c.POINTER(U32), c.POINTER(I64), OUT),
                      VIDEO_STREAM, 0, None, c.byref(flags), c.byref(pts), c.byref(sample))
            delivered = time.perf_counter_ns()
            if self.diagnostic:
                self.diagnostic.timing("mfReadSampleMs", delivered - read_at)
            try:
                if flags.value:
                    self.flags[flags.value] = self.flags.get(flags.value, 0) + 1
                check(hr, "stream-interrupted")
                if flags.value & 0x01:
                    raise CameraError("stream-interrupted")
                if flags.value & 0x02:
                    if self.file_source:
                        return None
                    raise CameraError("device-lost")
                if flags.value & 0x30:
                    try:
                        self.layout = video_layout(self.reader, self.platform)
                    except CameraError as error:
                        if error.code != "unsupported-format" or not flags.value & 0x10:
                            raise
                        self.layout = configure(self.reader, self.platform, self.diagnostic, not self.file_source)
                        continue
                if not sample or stop.is_set():
                    continue
                self.samples_read += 1
                if self.last_pts is not None and pts.value <= self.last_pts:
                    self.skipped += 1
                    continue
                self.last_pts = pts.value
                acquired_ns = delivered
                if not self.file_source:
                    observed_offset = delivered - pts.value * 100
                    self.offset_ns = observed_offset if self.offset_ns is None else min(self.offset_ns, observed_offset)
                    acquired_ns = pts.value * 100 + self.offset_ns
                    if delivered - acquired_ns >= 250_000_000:
                        self.skipped += 1
                        continue
                else:
                    self.next_frame = delivered / 1e9 + 0.08
                try:
                    pixels = sample_pixels(sample, self.np, *self.layout)
                except CameraError as error:
                    raise CameraError("frame-conversion-failed", error.native_code) from error
                self.last_sample_at = time.monotonic()
                self.converted += 1
                if self.diagnostic:
                    self.diagnostic.timing("nativeCopyMs", time.perf_counter_ns() - delivered)
                    self.last_frame_ms = self.diagnostic.elapsed()
                    if self.converted == 1:
                        self.diagnostic.data["firstFrameMs"] = self.last_frame_ms
                        self.diagnostic.save()
                return Frame(pixels, acquired_ns, bgr=True)
            finally:
                release(sample)
        return None


@contextmanager
def open_source(np, device=None, fixture_file=None, diagnostic=None):
    """Open a selected camera, or an explicit local test file through the same native reader."""
    stage = diagnostic.stage if diagnostic else lambda _: nullcontext()
    with ExitStack() as stack:
        with stage("MFStartup"):
            platform = stack.enter_context(foundation())
        readwrite = c.WinDLL("mfreadwrite")
        attrs = attributes(stack, platform)
        set_u32(attrs, VIDEO_PROCESSING, 1)
        set_u32(attrs, LOW_LATENCY, 1)
        reader = PTR()
        if fixture_file is not None:
            check(export(readwrite, "MFCreateSourceReaderFromURL", c.c_wchar_p, PTR, OUT)(
                str(fixture_file), attrs, c.byref(reader)))
        else:
            with stage("MFEnumDeviceSources"):
                selected = devices(platform).get(device)
                if selected is None:
                    raise CameraError("no-device")
            source_attrs = device_attributes(stack, platform)
            check(call(source_attrs, 25, (PTR, c.c_wchar_p), SYMBOLIC_LINK, selected[1]))
            set_u32(source_attrs, MAX_VIDEO_BUFFERS, 2)
            source = PTR()
            with stage("MFCreateDeviceSource"):
                check(export(c.WinDLL("mf"), "MFCreateDeviceSource", PTR, OUT)(source_attrs, c.byref(source)),
                      "device-activation-failed")
            stack.callback(release, source)
            def shutdown():
                try:
                    with stage("IMFMediaSource::Shutdown"):
                        check(call(source, 12, ()))
                except CameraError as error:
                    # Preserve an earlier capture failure. Process exit is still required by the host.
                    if diagnostic:
                        diagnostic.data["shutdownHresult"] = f"0x{error.native_code:08X}"
            stack.callback(shutdown)
            set_u32(attrs, DISCONNECT_ON_SHUTDOWN, 1)  # This scope explicitly shuts down the source.
            with stage("MFCreateSourceReaderFromMediaSource"):
                check(export(readwrite, "MFCreateSourceReaderFromMediaSource", PTR, PTR, OUT)(
                    source, attrs, c.byref(reader)), "source-reader-failed")
        stack.callback(release, reader)
        with stage("IMFSourceReader::SetCurrentMediaType"):
            layout = configure(reader, platform, diagnostic, fixture_file is None)
        capture = CameraFrameSource(reader, layout, np, fixture_file is not None, platform, diagnostic)
        try:
            yield capture
        finally:
            if diagnostic:
                diagnostic.data.update(samples=capture.samples_read, converted=capture.converted,
                                       skipped=capture.skipped, flags=capture.flags,
                                       lastFrameMs=capture.last_frame_ms)
