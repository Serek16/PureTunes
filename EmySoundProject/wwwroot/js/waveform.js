let ws
let wsRegions
let loop = true
let regionsToAdd = []

// Called from WaveformDisplay.razor
async function initializeWaveform(audioFileUrl, audioPeaks) {
    // Import the required modules using dynamic import
    const { default: WaveSurfer } = await import('https://unpkg.com/wavesurfer.js@7/dist/wavesurfer.esm.js')
    const { default: RegionsPlugin } = await import('https://unpkg.com/wavesurfer.js@7/dist/plugins/regions.esm.js')
    const { default: TimelinePlugin } = await import('https://unpkg.com/wavesurfer.js@7/dist/plugins/timeline.esm.js')
    const { default: HoverPlugin } = await import('https://unpkg.com/wavesurfer.js@7/dist/plugins/hover.esm.js')
    const { default: ZoomPlugin } = await import('https://unpkg.com/wavesurfer.js@7/dist/plugins/zoom.esm.js')

    // Create your own media element
    const audio = new Audio()
    audio.src = audioFileUrl
    audio.controls = true
    audio.style.width = "100%"

    // Create an instance of WaveSurfer
    ws = WaveSurfer.create({
        container: '#waveform',
        waveColor: 'rgb(200, 0, 200)',
        progressColor: 'rgb(200, 0, 200)',
        media: audio,
        barHeight: 1,
        peaks: audioPeaks,
        minPxPerSec: 1,
        dragToSeek: true,
    })

    // Initialize the Timeline plugin
    ws.registerPlugin(TimelinePlugin.create())

    // Initialize the Zoom plugin
    ws.registerPlugin(
        ZoomPlugin.create({
            // the amount of zoom per wheel step, e.g. 0.1 means a 10% magnification per scroll
            scale: 0.05,
        }),
    )

    // Initialize the Regions plugin
    wsRegions = ws.registerPlugin(RegionsPlugin.create())

    // Update the zoom level on slider change
    ws.once('decode', () => {
        document.querySelector('input[type="range"]').oninput = (e) => {
            const minPxPerSec = Number(e.target.value)
            ws.zoom(minPxPerSec)
        }
    })

    const slider = document.getElementById('slider')

    // This will be executed after the waveform is fully drawn
    ws.once('ready', () => {
        // Add Audio controls
        const audioElement = document.getElementById("audioControls")
        audioElement.appendChild(audio)

        ws.registerPlugin(HoverPlugin.create({
            lineColor: '#ff0000',
            lineWidth: 2,
            labelBackground: '#555',
            labelColor: '#fff',
            labelSize: '11px',
        }))
        
        placeRegions()

        let duration = ws.options.media.duration
        let width = ws.renderer.container.clientWidth
        let minPxPerSec = Math.round(width / duration)

        ws.options.minPxPerSec = minPxPerSec
        slider.min = minPxPerSec
        slider.value = minPxPerSec
    })

    wsRegions.on('region-double-clicked', (region, e) => {
        e.preventDefault()
        e.stopPropagation()
        zoomToRegion(region)
    })

    // Setting max and min zoom level.
    ws.on('zoom', () => {
        let minPxPerSec =  Math.round(ws.options.minPxPerSec)

        if (minPxPerSec > 200) {
            minPxPerSec = 200
            ws.options.minPxPerSec = minPxPerSec
        }

        slider.value = minPxPerSec
    })
}

// Called from WaveformDisplay.razor
function setLoopStatus(status) {
    loop = status
}

function addRegion(start, end, name) {
    wsRegions.addRegion({
        start: start,
        end: end,
        content: name,
        color: "rgba(10,10,10,0.5)",
        drag: false,
        resize: false,
    })
}

function placeRegions() {
    regionsToAdd.forEach(regionInfo => {
        addRegion(regionInfo.start, regionInfo.end, regionInfo.name)
    })
}

// Called from WaveformDisplay.razor
function addRegionToList(start, end, name) {
    let regionInfo = {
        'start': start,
        'end': end,
        'name': name
    }
    regionsToAdd.push(regionInfo)
}

async function zoomToRegion(region) {
    // Calculate the region's length in seconds
    let regionDuration = region.end - region.start

    // Zoom that the region takes up 50% of the curren waveform width
    let zoomVal = (ws.renderer.container.clientWidth * 0.5) / regionDuration
    ws.zoom(zoomVal)

    // Set cursor to the start of the region
    ws.setTime(region.start)

    // Set view on region in the center
    let scrollbar = document.querySelector('#waveform > div').shadowRoot.querySelector('.scroll')
    scrollbar.scrollLeft = ((region.start - regionDuration / 2) * scrollbar.scrollWidth / ws.options.media.duration)

    refreshWaveform()
}

function delay(time) {
    return new Promise(resolve => setTimeout(resolve, time));
}

function refreshWaveform() {
    const currentOptions = {
        waveColor: ws.options.waveColor,
        height: ws.options.height
    }
    ws.setOptions(currentOptions)
}