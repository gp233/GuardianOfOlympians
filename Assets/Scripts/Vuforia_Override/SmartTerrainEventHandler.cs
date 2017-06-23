/*==============================================================================
Copyright (c) 2013-2014 Qualcomm Connected Experiences, Inc.
All Rights Reserved.
==============================================================================*/

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Vuforia;

/// <summary>
///  A custom handler that implements the ITrackerEventHandler interface.
/// </summary>
public class SmartTerrainEventHandler : MonoBehaviour
{

    #region PRIVATE_MEMBERS

    private bool m_propsCloned;
    private bool DT_Display = true;
    private ReconstructionBehaviour mReconstructionBehaviour;
    SmartTerrainTracker DT_Tracker;

    //�����Ƿ񱻷���
    private bool surfaceFound;

    //prop����
    private PropAbstractBehaviour[] props;
    //surface����
    private SurfaceAbstractBehaviour[] surfaces;

    #endregion //PRIVATE MEMBERS

    #region PUBLIC_MEMBERS

    //�۲���ģʽ
    public delegate void SurfaceFoundForFirstTime(GameObject surface);
    public static event SurfaceFoundForFirstTime OnFoundSurface;

    public PropBehaviour PropTemplate;
    public SurfaceBehaviour SurfaceTemplate;

    //��¥ģ��
    public GameObject tower;

    public bool propsCloned
    {
        get
        {
            return m_propsCloned;
        }
    }

    //��ɨ�����¥�������п���
    const int maxProp = 3;
    public int num = 0;
    //��������߽����
    const float offset = 0.5f;

    #endregion

    #region UNITY_MONOBEHAVIOUR

    //�Իص���������ע��
    void Start() {
        //��ȡSmart Terrain Tracker �� Reconstruction
        mReconstructionBehaviour = GetComponent<ReconstructionBehaviour>();
        DT_Tracker = TrackerManager.Instance.GetTracker<SmartTerrainTracker>();

        if (mReconstructionBehaviour) {
            mReconstructionBehaviour.RegisterInitializedCallback(OnInitialized);
            mReconstructionBehaviour.RegisterPropCreatedCallback(OnPropCreated);
            mReconstructionBehaviour.RegisterSurfaceCreatedCallback(OnSurfaceCreated);
        }
    }

    //�Իص���������ע��
    void OnDestroy() {
        if (mReconstructionBehaviour) {
            mReconstructionBehaviour.UnregisterInitializedCallback(OnInitialized);
            mReconstructionBehaviour.UnregisterPropCreatedCallback(OnPropCreated);
            mReconstructionBehaviour.UnregisterSurfaceCreatedCallback(OnSurfaceCreated);
        }
    }

    #endregion //UNITY_MONOBEHAVIOUR

    #region ISmartTerrainEventHandler_Implementations

    //smart terrain����Target���г�ʼ��
    public void OnInitialized(SmartTerrainInitializationInfo initializationInfo) {
        Debug.Log("Finished initializing at [" + Time.time + "]");
    }

    //�Ա��������ɽ��м����ص�
    public void OnPropCreated(Prop prop) {
        //�˴����Ը���prop�����ϢΪ����ز�ͬ����Ϸ�߼�
        //ͬʱ����prop�������м��
        if (mReconstructionBehaviour && num < maxProp) {
            mReconstructionBehaviour.AssociateProp(PropTemplate, prop);
            PropAbstractBehaviour behaviour;
            if (mReconstructionBehaviour.TryGetPropBehaviour(prop, out behaviour))
                behaviour.gameObject.name = "Prop " + prop.ID;
            //���ӱ�������
            num++;
        }
    }

    //��smart terrain�����ɽ��м����ص�
    public void OnSurfaceCreated(Surface surface) {
        //shows an example of how you could get a handle on the surface game objects to perform different game logic
        if (mReconstructionBehaviour) {
            mReconstructionBehaviour.AssociateSurface(SurfaceTemplate, surface);
            SurfaceAbstractBehaviour behaviour;
            if (mReconstructionBehaviour.TryGetSurfaceBehaviour(surface, out behaviour)) {
                behaviour.gameObject.name = "Primary " + surface.ID;
                //��һ�η���֪ͨ����
                if (!surfaceFound) {
                    OnFoundSurface(behaviour.gameObject);
                    surfaceFound = true;
                }
            }
        }
    }

    #endregion // ISmartTerrainEventHandler_Implementations

    #region PUBLIC_METHODS

    //��ʵ�ｨģ��ȾΪ��Ϸ����
    public void ShowPropClones() {
        if (!m_propsCloned) {
            //��prop�����û�
            PropAbstractBehaviour[] props = gameObject.GetComponentsInChildren<PropAbstractBehaviour>();
         
            foreach (PropAbstractBehaviour prop in props) {
                // ��¥ʵ����
                GameObject Tower = Instantiate(tower);
                //����prop��
                Tower.gameObject.transform.SetParent(prop.gameObject.transform);
                //��ʼ��
                Vector3 scale = prop.Prop.BoundingBox.HalfExtents;
                float length = Mathf.Max(scale.x, scale.z);
                //���ñ�Ե��ʹ����ȫ������
                Tower.transform.localScale = new Vector3(length * 10 + offset, scale.y * 6, length * 10 + offset);
                Tower.transform.eulerAngles = new Vector3(0, prop.Prop.BoundingBox.RotationY, 0);
                
                //��������Ч��Χ��������
                ParticleSystem[] particle = Tower.transform.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem p in particle) {
                    ParticleSystem.ShapeModule shapeModule = p.shape;
                    shapeModule.radius = shapeModule.radius * length * 10;
                }

                //����λ������
                Vector3 pos = prop.Prop.BoundingBox.Center;
                Tower.transform.localPosition = new Vector3(pos.x, 0, pos.z);

                Debug.Log(Tower.transform.position + "   " + length * 2);

                //���Ҽ����ͼռ�����
                StartCoroutine(Singleton<MapController>.Instance.CoverMap(Tower.transform.position.x, Tower.transform.position.z, length*2));

                //�Զ������в���
                Tower.GetComponent<grow>().Play();
            }
          
            m_propsCloned = true;
        }
    }

    //ֹͣprop��surface�ĸ���
    public void StopUpdate() {
        props = gameObject.GetComponentsInChildren<PropAbstractBehaviour>();
        foreach (PropAbstractBehaviour prop in props)
            //ֹͣ���µ�����׷��
            prop.SetAutomaticUpdatesDisabled(true);
       
        surfaces = gameObject.GetComponentsInChildren<SurfaceAbstractBehaviour>();
        foreach (SurfaceAbstractBehaviour surface in surfaces)
            //�ر�surface�ĸ���
            surface.SetAutomaticUpdatesDisabled(true);
    }

    //�����ɱ���ǰ��ʵ�ｨģ��prop����ȥ��
    public void Hide() {
        if (DT_Display) {
            //��prop��������
            props = gameObject.GetComponentsInChildren<PropAbstractBehaviour>();
            foreach (PropAbstractBehaviour prop in props) {
                //�����Ľ�ģ��ȥ��
                Renderer propRenderer = prop.GetComponent<MeshRenderer>();
                if (propRenderer != null) {
                    //ȥ��mesh��ģ
                    Destroy(propRenderer);
                }
            }

            //��surface�����û�
            SurfaceAbstractBehaviour[] surfaces = gameObject.GetComponentsInChildren<SurfaceAbstractBehaviour>();

            foreach (SurfaceAbstractBehaviour surface in surfaces) {
                Renderer surfaceRenderer = surface.GetComponent<MeshRenderer>();   
                //ȥ��smart terrain��Render
                if (surfaceRenderer != null) {
                    Destroy(surfaceRenderer);
                }
            }

            DT_Display = false;
        }
    }

    //��ʵ�ｨģ����ˢ��
    public void Refresh() {
              
        if ((mReconstructionBehaviour != null) && (mReconstructionBehaviour.Reconstruction != null)) {
            bool trackerWasActive = DT_Tracker.IsActive;
            // ֹͣSmart Terrain Tracker
            if (trackerWasActive)
                DT_Tracker.Stop();
            // ����Reconstruction
            mReconstructionBehaviour.Reconstruction.Reset();
            // ���½���ɨ����
            if (trackerWasActive) {
                DT_Tracker.Start();
                mReconstructionBehaviour.Reconstruction.Start();
            }
        }

        //���ñ���Ϊδ����
        surfaceFound = false;
        //������Ϊ0
        num = 0;
    }

    #endregion //PUBLIC_METHODS
}



